using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;
using Microsoft.Data.Sqlite;
using MentoringApp.Data.DTO;
using System.Text.RegularExpressions;
using MentoringApp.Model.User;

namespace MentoringApp.Data.Acess.SQLite
{
    
    internal class SqlUserRepo : IUserRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlUserRepo(ISQLiteConnectionService db, string connectionString)
        {
            _db = db;
        }

        public async Task<IEnumerable<SupervisorStatsDto>> GetSupervisorStatisticsAsync()
        {
            const string sql = @"
                SELECT 
                    u.Id, 
                    u.Name AS UserName,
                    (SELECT COUNT(*) 
                        FROM Issues i 
                        JOIN Pairs p ON i.PairId = p.Id 
                        WHERE p.SupervisorId = u.Id AND i.Status = 'Pending') AS PendingIssuesCount,
                    (SELECT COUNT(*) 
                        FROM Pairs p 
                        WHERE p.SupervisorId = u.Id) AS PairsCount
                FROM Users u
                INNER JOIN UserSupervisors us ON u.Id = us.UserId";

            return await _db.QueryAsync<SupervisorStatsDto>(sql);
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
        INSERT INTO Users (UserName, Email, NationalId, ProfilePicturePath) 
        VALUES (@UserName, @Email, @NationalId, @ProfilePicturePath);

        /* 2. Get the generated ID and use it for role tables */
        /* We use the WHERE clause as an 'if' statement inside SQL */

        INSERT INTO UserStudents (UserId, GradeId)
        SELECT last_insert_rowid(), @GradeId WHERE @IsStudent = 1;

        INSERT INTO UserMentors (UserId, SubjectToTeach)
        SELECT last_insert_rowid(), @MentorSubId WHERE @IsMentor = 1;

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
                IsStudent = isStudent ? 1 : 0,
                IsMentor = isMentor ? 1 : 0,
                IsMentee = isMentee ? 1 : 0,
                IsSupervisor = isSupervisor ? 1 : 0,
                IsAdmin = isAdmin ? 1 : 0,
                GradeId = (user as StudentModel)?.Grade.Id ?? 0,
                MentorSubId = (user as StudentModel)?.MentorProfile?.SubjectToTeach ?? 0,
                MenteeSubId = (user as StudentModel)?.MenteeProfile?.SubjectToLearn ?? 0,
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


        private async Task<UserRoleType> DetermineRoleAsync(int userId)
        {
            var role = UserRoleType.Student;
            if (await IsAdmin(userId)) role = UserRoleType.Admin;
            else if (await IsSupervisor(userId)) role = UserRoleType.Supervisor;
            return role;
        }

        public async Task<UserDto?> GetUserDtoByNationalIdAsync(string nationalId)
        {
            var userRow = await _db.QuerySingleAsync<UserRow>(
                "SELECT * FROM Users WHERE NationalId = @NationalId",
                new { NationalId = nationalId });

            if (userRow == null) return null;

            return await MapToFullDtoAsync(userRow);
        }

        public async Task<UserDto?> GetUserDtoByIdAsync(int userId)
        {
            var userRow = await _db.QuerySingleAsync<UserRow>(
                "SELECT * FROM Users WHERE Id = @Id",
                new { Id = userId });

            if (userRow == null) return null;

            return await MapToFullDtoAsync(userRow);
        }

        private async Task<UserDto> MapToFullDtoAsync(UserRow userRow)
        {
            var userId = userRow.Id;

            var studentData = await _db.QuerySingleAsync<StudentRow>("SELECT * FROM UserStudents WHERE UserId = @Id", new { Id = userId });
            var mentorData = await _db.QuerySingleAsync<MentorRow>("SELECT * FROM UserMentors WHERE UserId = @Id", new { Id = userId });
            var menteeData = await _db.QuerySingleAsync<MenteeRow>("SELECT * FROM UserMentees WHERE UserId = @Id", new { Id = userId });
            var codeData = await _db.QuerySingleAsync<CodeRow>("SELECT * FROM VerificationCodes WHERE UserId = @Id", new { Id = userId });

            UserRoleType roleType = await DetermineRoleAsync(userId);

            return new UserDto
            {
                Id = userRow.Id,
                UserName = userRow.UserName,
                Email = userRow.Email,
                NationalId = userRow.NationalId,
                ProfilePicturePath = userRow.ProfilePicturePath,
                Role = roleType,
                GradeId = studentData?.GradeId,
                MentorSubjectId = mentorData?.SubjectToTeach,
                MenteeSubjectId = menteeData?.SubjectToLearn,
                VerificationCode = codeData?.Code,
                VerificationCodeCreated = codeData != null ? DateTime.Parse(codeData.CreationDate) : null
            };
        }

        public async Task<IEnumerable<UserDto>> GetAllUserDtosAsync()
        {
            var users = await _db.QueryAsync<UserRow>("SELECT * FROM Users");

            var students = (await _db.QueryAsync<StudentRow>("SELECT * FROM UserStudents")).ToDictionary(s => s.UserId);
            var mentors = (await _db.QueryAsync<MentorRow>("SELECT * FROM UserMentors")).ToDictionary(m => m.UserId);
            var mentees = (await _db.QueryAsync<MenteeRow>("SELECT * FROM UserMentees")).ToDictionary(m => m.UserId);

            var tasks = users.Select(async u =>
            {
                return new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    NationalId = u.NationalId,
                    ProfilePicturePath = u.ProfilePicturePath,
                    Role = await DetermineRoleAsync(u.Id),
                    GradeId = students.TryGetValue(u.Id, out var s) ? s.GradeId : null,
                    MentorSubjectId = mentors.TryGetValue(u.Id, out var m) ? m.SubjectToTeach : null,
                    MenteeSubjectId = mentees.TryGetValue(u.Id, out var me) ? me.SubjectToLearn : null
                };
            });

            return await Task.WhenAll(tasks);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            int rows = await _db.ExecuteAsync("DELETE FROM Users WHERE Id = @Id", new { Id = userId });
            return rows > 0;
        }

        public async Task<bool> UpdateBaseInfoAsync(int id, string name, string email, string nationalId)
        {
            const string sql = "UPDATE Users SET UserName = @name, Email = @email, NationalId = @nationalId WHERE Id = @id";
            return await _db.ExecuteAsync(sql, new { id, name, email, nationalId }) > 0;
        }

        public async Task UpdateStudentGradeAsync(int userId, int gradeId)
        {
            const string sql = "UPDATE UserStudents SET GradeId = @gradeId WHERE UserId = @userId";
            await _db.ExecuteAsync(sql, new { userId, gradeId });
        }


        private class UserRow
        {
            public int Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string NationalId { get; set; } = string.Empty;
            public string? ProfilePicturePath { get; set; }
        }



        private class StudentRow
        {
            public int UserId { get; set; }
            public int GradeId { get; set; }
        }

        private class MentorRow
        {
            public int UserId { get; set; }
            public int SubjectToTeach { get; set; }
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

    }
}
