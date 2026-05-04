using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;
using Microsoft.Data.Sqlite;
using MentoringApp.Data.DTO;
using System.Text.RegularExpressions;
using MentoringApp.Model.User;
using MentoringApp.Data.Dao;

namespace MentoringApp.Data.Acess.SQLite
{
    
    /// <summary>
    /// SQLite implementation of <see cref="IUserRepo"/>.
    /// Users are stored in a vertical partition: a shared <c>Users</c> table holds identity data,
    /// while <c>UserStudents</c>, <c>UserMentors</c>, <c>UserMentees</c>, <c>UserSupervisors</c>,
    /// and <c>UserAdmins</c> hold role-specific columns.
    /// <see cref="MapToFullDtoAsync"/> assembles a complete <see cref="UserDao"/> from these tables.
    /// </summary>
    internal class SqlUserRepo : IUserRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlUserRepo(ISQLiteConnectionService db, string connectionString)
        {
            _db = db;
        }

        public async Task<IEnumerable<SupervisorStatsDao>> GetSupervisorStatisticsAsync()
        {
            const string sql = @"
                SELECT
                    u.Id,
                    u.UserName,
                    (SELECT COUNT(*)
                        FROM Issues i
                        WHERE i.ReportedByUserId IN (
                            SELECT MentorId FROM Pairs WHERE SupervisorId = u.Id
                            UNION
                            SELECT MenteeId FROM Pairs WHERE SupervisorId = u.Id
                        )
                        AND i.IsResolved = 0) AS PendingIssuesCount,
                    (SELECT COUNT(*)
                        FROM Issues i
                        WHERE i.ReportedByUserId IN (
                            SELECT MentorId FROM Pairs WHERE SupervisorId = u.Id
                            UNION
                            SELECT MenteeId FROM Pairs WHERE SupervisorId = u.Id
                        )
                        AND i.IsResolved = 1) AS ResolvedIssuesCount,
                    (SELECT COUNT(*) FROM Pairs p WHERE p.SupervisorId = u.Id) AS PairsCount
                FROM Users u
                INNER JOIN UserSupervisors us ON u.Id = us.UserId";

            return await _db.QueryAsync<SupervisorStatsDao>(sql);
        }



        private async Task<bool> IsAdmin(int userId)
        {
            var row = await _db.QuerySingleAsync<CountRow>(
                "SELECT COUNT(1) AS Count FROM UserAdmins WHERE UserId = @UserId",
                new { UserId = userId });

            return row != null && row.Count > 0;
        }

        private async Task<bool> IsSupervisor(int userId)
        {
            var row = await _db.QuerySingleAsync<CountRow>(
                "SELECT COUNT(1) AS Count FROM UserSupervisors WHERE UserId = @UserId",
                new { UserId = userId });

            return row != null && row.Count > 0;
        }
        private class CountRow
        {
            public int Count { get; set; }
        }

        public async Task<bool> CreateUserAsync(UserModel user)
        {
            // 1. Determine which role flags to set for the SQL logic
            bool isStudent = user is StudentModel;
            bool isMentor = (user as StudentModel)?.IsMentor ?? false;
            bool isMentee = (user as StudentModel)?.IsMentee ?? false;
            bool isSupervisor = user is SupervisorModel;
            bool isAdmin = user is AdminModel;

            const string sql = @"
        /* 1. Create the base user */
        INSERT INTO Users (UserName, Email, NationalId, ProfilePicturePath, Language, PhoneNumber, Gender)
        VALUES (@UserName, @Email, @NationalId, @ProfilePicturePath, @Language, @PhoneNumber, @Gender);

        /* 2. Get the generated ID and use it for role tables */
        /* We use the WHERE clause as an 'if' statement inside SQL */

        INSERT INTO UserStudents (UserId, GradeId, ClassNum, PreferredMentorGender, PreferredMenteeGender)
        SELECT last_insert_rowid(), @GradeId, @StudentClassNum, @PreferredMentorGender, @PreferredMenteeGender WHERE @IsStudent = 1;

        INSERT INTO UserMentors (UserId, SubjectToTeach, MaxMentees)
        SELECT last_insert_rowid(), @MentorSubId, @MaxMentees WHERE @IsMentor = 1;

        INSERT INTO UserMentees (UserId, SubjectToLearn)
        SELECT last_insert_rowid(), @MenteeSubId WHERE @IsMentee = 1;

        INSERT INTO UserSupervisors (UserId)
        SELECT last_insert_rowid() WHERE @IsSupervisor = 1;

        INSERT INTO UserAdmins (UserId)
        SELECT last_insert_rowid() WHERE @IsAdmin = 1;

        /* 3. Handle Verification Code if provided */
        INSERT INTO VerificationCodes (UserId, Code, CreationDate)
        SELECT last_insert_rowid(), @Code, @Date WHERE @HasCode = 1;

        /* Return the new ID so we can update the user object in memory */
        SELECT last_insert_rowid() AS Id;";

            // 2. Map all possible parameters (defaults to 0/null if not applicable)
            var parameters = new
            {
                user.UserName,
                user.Email,
                user.NationalId,
                ProfilePicturePath = user.ProfilePicturePath,
                Language = user.Language,
                PhoneNumber = user.PhoneNumber,
                Gender = (int)user.Gender,
                IsStudent = isStudent ? 1 : 0,
                IsMentor = isMentor ? 1 : 0,
                IsMentee = isMentee ? 1 : 0,
                IsSupervisor = isSupervisor ? 1 : 0,
                IsAdmin = isAdmin ? 1 : 0,
                GradeId = (user as StudentModel)?.Grade.Id ?? 0,
                StudentClassNum = (user as StudentModel)?.ClassNum ?? 0,
                PreferredMentorGender = (int)((user as StudentModel)?.PreferredMentorGender ?? MentoringApp.Model.User.GenderPreference.NoPreference),
                PreferredMenteeGender = (int)((user as StudentModel)?.PreferredMenteeGender ?? MentoringApp.Model.User.GenderPreference.NoPreference),
                MentorSubId = (user as StudentModel)?.MentorProfile?.SubjectToTeach ?? 0,
                MaxMentees = (user as StudentModel)?.MentorProfile?.MaxMentees ?? 1,
                MenteeSubId = (user as StudentModel)?.MenteeProfile?.SubjectToLearn ?? 0,
                SupGradeId = 0,
                SupClassNum = 0,
                HasCode = user.CurrentVerificationCode != null ? 1 : 0,
                Code = user.CurrentVerificationCode?.Code,
                Date = user.CurrentVerificationCode?.CreationDate.ToString("o")
            };

            // 3. Single execution via your DB service
            var result = await _db.QuerySingleAsync<IdRow>(sql, parameters);

            if (result?.Id > 0)
            {
                user.Id = (int)(result.Id);
                return true;
            }

            return false;
        }
        private class IdRow
        {
            public long Id { get; set; }
        }




        public async Task UpsertMentorProfileAsync(int userId, int subjectId)
        {
            const string sql = @"
        INSERT INTO UserMentors (UserId, SubjectToTeach)
        VALUES (@userId, @subjectId)
        ON CONFLICT(UserId) DO UPDATE SET SubjectToTeach = excluded.SubjectToTeach";

            await _db.ExecuteAsync(sql, new { userId, subjectId });
        }

        public async Task UpsertMenteeProfileAsync(int userId, int subjectId)
        {
            const string sql = @"
        INSERT INTO UserMentees (UserId, SubjectToLearn)
        VALUES (@userId, @subjectId)
        ON CONFLICT(UserId) DO UPDATE SET SubjectToLearn = excluded.SubjectToLearn";

            await _db.ExecuteAsync(sql, new { userId, subjectId });
        }


        /// <summary>
        /// Resolves role by presence in the role tables: Admin trumps Supervisor, both trump Student.
        /// Student is the implicit default when neither role table has a row for the user.
        /// </summary>
        private async Task<UserRoleType> DetermineRoleAsync(int userId)
        {
            var role = UserRoleType.Student;
            if (await IsAdmin(userId)) role = UserRoleType.Admin;
            else if (await IsSupervisor(userId)) role = UserRoleType.Supervisor;
            return role;
        }

        public async Task<UserDao?> GetUserDtoByNationalIdAsync(string nationalId)
        {
            var userRow = await _db.QuerySingleAsync<UserRow>(
                "SELECT * FROM Users WHERE NationalId = @NationalId",
                new { NationalId = nationalId });

            if (userRow == null) return null;

            return await MapToFullDtoAsync(userRow);
        }

        public async Task<UserDao?> GetUserDtoByIdAsync(int userId)
        {
            var userRow = await _db.QuerySingleAsync<UserRow>(
                "SELECT * FROM Users WHERE Id = @Id",
                new { Id = userId });

            if (userRow == null) return null;

            return await MapToFullDtoAsync(userRow);
        }

        private async Task<UserDao> MapToFullDtoAsync(UserRow userRow)
        {
            var userId = userRow.Id;

            var studentData = await _db.QuerySingleAsync<StudentRow>("SELECT * FROM UserStudents WHERE UserId = @Id", new { Id = userId });
            var mentorData = await _db.QuerySingleAsync<MentorRow>("SELECT * FROM UserMentors WHERE UserId = @Id", new { Id = userId });
            var menteeData = await _db.QuerySingleAsync<MenteeRow>("SELECT * FROM UserMentees WHERE UserId = @Id", new { Id = userId });
            var supervisorData = await _db.QuerySingleAsync<SupervisorRow>("SELECT * FROM UserSupervisors WHERE UserId = @Id", new { Id = userId });
            var codeData = await _db.QuerySingleAsync<CodeRow>("SELECT * FROM VerificationCodes WHERE UserId = @Id", new { Id = userId });

            UserRoleType roleType = await DetermineRoleAsync(userId);

            return new UserDao
            {
                Id = userRow.Id,
                UserName = userRow.UserName,
                Email = userRow.Email,
                NationalId = userRow.NationalId,
                ProfilePicturePath = userRow.ProfilePicturePath,
                Language = userRow.Language ?? "en",
                PhoneNumber = userRow.PhoneNumber,
                Gender = userRow.Gender,
                Role = roleType,
                GradeId = roleType == UserRoleType.Supervisor ? null : studentData?.GradeId,
                ClassNum = roleType == UserRoleType.Supervisor ? null : studentData?.ClassNum,
                PreferredMentorGender = roleType == UserRoleType.Student ? studentData?.PreferredMentorGender : null,
                PreferredMenteeGender = roleType == UserRoleType.Student ? studentData?.PreferredMenteeGender : null,
                MentorSubjectId = mentorData?.SubjectToTeach,
                MaxMentees = mentorData?.MaxMentees,
                MenteeSubjectId = menteeData?.SubjectToLearn,
                VerificationCode = codeData?.Code,
                VerificationCodeCreated = codeData != null ? DateTime.Parse(codeData.CreationDate) : null
            };
        }

        /// <summary>
        /// Bulk-loads all role tables up front into dictionaries to avoid N+1 queries,
        /// then projects each user row into a <see cref="UserDao"/> in parallel.
        /// Role determination still requires two async checks per user (IsAdmin / IsSupervisor).
        /// </summary>
        public async Task<IEnumerable<UserDao>> GetAllUserDtosAsync()
        {
            var users = await _db.QueryAsync<UserRow>(
                "SELECT * FROM Users WHERE Id NOT IN (SELECT UserId FROM UserAdmins)");

            // Load all role tables once and index by UserId for O(1) lookups below
            var students = (await _db.QueryAsync<StudentRow>("SELECT * FROM UserStudents")).ToDictionary(s => s.UserId);
            var mentors = (await _db.QueryAsync<MentorRow>("SELECT * FROM UserMentors")).ToDictionary(m => m.UserId);
            var mentees = (await _db.QueryAsync<MenteeRow>("SELECT * FROM UserMentees")).ToDictionary(m => m.UserId);
            var supervisors = (await _db.QueryAsync<SupervisorRow>("SELECT * FROM UserSupervisors")).ToDictionary(s => s.UserId);

            var tasks = users.Select(async u =>
            {
                var role = await DetermineRoleAsync(u.Id);
                bool isSupervisor = role == UserRoleType.Supervisor;
                students.TryGetValue(u.Id, out var studentRow);
                mentors.TryGetValue(u.Id, out var mentorRow);
                mentees.TryGetValue(u.Id, out var menteeRow);
                return new UserDao
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    NationalId = u.NationalId,
                    ProfilePicturePath = u.ProfilePicturePath,
                    PhoneNumber = u.PhoneNumber,
                    Gender = u.Gender,
                    Role = role,
                    GradeId = isSupervisor ? null : studentRow?.GradeId,
                    ClassNum = isSupervisor ? null : studentRow?.ClassNum,
                    PreferredMentorGender = isSupervisor ? null : (int?)studentRow?.PreferredMentorGender,
                    PreferredMenteeGender = isSupervisor ? null : (int?)studentRow?.PreferredMenteeGender,
                    MentorSubjectId = mentorRow?.SubjectToTeach,
                    MaxMentees = mentorRow?.MaxMentees,
                    MenteeSubjectId = menteeRow?.SubjectToLearn
                };
            });

            return await Task.WhenAll(tasks);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            const string sql = @"
                DELETE FROM UserStudents WHERE UserId = @Id;
                DELETE FROM UserMentors WHERE UserId = @Id;
                DELETE FROM UserMentees WHERE UserId = @Id;
                DELETE FROM UserSupervisors WHERE UserId = @Id;
                DELETE FROM UserAdmins WHERE UserId = @Id;
                DELETE FROM VerificationCodes WHERE UserId = @Id;
                
                DELETE FROM Reviews WHERE AuthorUserId = @Id;
                DELETE FROM Issues WHERE ReportedByUserId = @Id;

                DELETE FROM Reviews WHERE PairId IN (SELECT Id FROM Pairs WHERE MentorId = @Id OR MenteeId = @Id OR SupervisorId = @Id);
                
                /* Some schema versions might have PairId in Issues, so we try to delete them if they exist. We'll just ignore errors if column doesn't exist, but it's easier to just do simple cleans. However since SQLite allows multiple statements, we can delete the pairs and anything depending on them will cascade if PRAGMA is ON. But wait, if we delete pairs first, we must catch any potential PairId without ON DELETE CASCADE */
                
                DELETE FROM Pairs WHERE MentorId = @Id OR MenteeId = @Id OR SupervisorId = @Id;

                DELETE FROM Users WHERE Id = @Id;";

            int rows = await _db.ExecuteAsync(sql, new { Id = userId });
            return rows > 0;
        }

        public async Task<bool> UpdateBaseInfoAsync(int id, string name, string email, string nationalId, string? phoneNumber, int gender)
        {
            const string sql = "UPDATE Users SET UserName = @name, Email = @email, NationalId = @nationalId, PhoneNumber = @phoneNumber, Gender = @gender WHERE Id = @id";
            return await _db.ExecuteAsync(sql, new { id, name, email, nationalId, phoneNumber, gender }) > 0;
        }

        public async Task UpdateStudentPreferredGendersAsync(int userId, int preferredMentorGender, int preferredMenteeGender)
        {
            const string sql = "UPDATE UserStudents SET PreferredMentorGender = @preferredMentorGender, PreferredMenteeGender = @preferredMenteeGender WHERE UserId = @userId";
            await _db.ExecuteAsync(sql, new { userId, preferredMentorGender, preferredMenteeGender });
        }

        public async Task UpdateStudentGradeAndClassAsync(int userId, int gradeId, int classNum)
        {
            const string sql = "UPDATE UserStudents SET GradeId = @gradeId, ClassNum = @classNum WHERE UserId = @userId";
            await _db.ExecuteAsync(sql, new { userId, gradeId, classNum });
        }

        public async Task UpdateSupervisorClassesAsync(int supervisorId, IEnumerable<int> schoolClassIds)
        {
            const string deleteSQL = "DELETE FROM SupervisorClasses WHERE SupervisorId = @supervisorId";
            await _db.ExecuteAsync(deleteSQL, new { supervisorId });
            foreach (var id in schoolClassIds)
            {
                const string insertSQL = "INSERT OR IGNORE INTO SupervisorClasses (SupervisorId, SchoolClassId) VALUES (@supervisorId, @schoolClassId)";
                await _db.ExecuteAsync(insertSQL, new { supervisorId, schoolClassId = id });
            }
        }


        private class UserRow
        {
            public int Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string NationalId { get; set; } = string.Empty;
            public string? ProfilePicturePath { get; set; }
            public string? Language { get; set; }
            public string? PhoneNumber { get; set; }
            public int Gender { get; set; } = 3;
        }



        private class StudentRow
        {
            public int UserId { get; set; }
            public int GradeId { get; set; }
            public int ClassNum { get; set; }
            public int PreferredMentorGender { get; set; } = 2;
            public int PreferredMenteeGender { get; set; } = 2;
        }

        private class MentorRow
        {
            public int UserId { get; set; }
            public int SubjectToTeach { get; set; }
            public int MaxMentees { get; set; }
        }

        private class SupervisorRow
        {
            public int UserId { get; set; }
            public int GradeId { get; set; }
            public int ClassNum { get; set; }
        }

        private class MenteeRow
        {
            public int UserId { get; set; }
            public int SubjectToLearn { get; set; }
        }

        private class CodeRow
        {
            public int UserId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string CreationDate { get; set; } = string.Empty;
        }

        public async Task<bool> UpdateProfilePictureAsync(int userId, string? path)
        {
            const string sql = "UPDATE Users SET ProfilePicturePath = @path WHERE Id = @userId";
            return await _db.ExecuteAsync(sql, new { userId, path }) > 0;
        }

        public async Task<bool> UpdateLanguageAsync(int userId, string language)
        {
            const string sql = "UPDATE Users SET Language = @language WHERE Id = @userId";
            return await _db.ExecuteAsync(sql, new { userId, language }) > 0;
        }

    }
}
