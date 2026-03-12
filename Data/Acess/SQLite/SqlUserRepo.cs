using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;
using Microsoft.Data.Sqlite;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlUserRepo : IUserRepo
    {
        private readonly ISQLiteConnectionService _db;
        private readonly string _connectionString;
        private readonly IGradeRepo _gradeRepo;

        public SqlUserRepo(ISQLiteConnectionService db, string connectionString, IGradeRepo gradeRepo)
        {
            _db = db;
            _connectionString = connectionString;
            _gradeRepo = gradeRepo;
        }

        public async Task<User?> LoadUserByNationalIdAsync(string nationalId)
        {
            var row = _db.QuerySingle<UserRow>(
                "SELECT Id, UserName, Email, NationalId FROM Users WHERE NationalId = @NationalId",
                new { NationalId = nationalId });
            return row == null ? null : await MapToDomainAsync(row);
        }

        public async Task<User?> LoadUserByIdAsync(int userId)
        {
            var row = _db.QuerySingle<UserRow>(
                "SELECT Id, UserName, Email, NationalId FROM Users WHERE Id = @Id",
                new { Id = userId });
            return row == null ? null : await MapToDomainAsync(row);
        }

        public bool UserExists(string nationalId)
        {
            var row = _db.QuerySingle<CountRow>(
                "SELECT COUNT(1) AS Count FROM Users WHERE NationalId = @NationalId",
                new { NationalId = nationalId });
            return row != null && row.Count > 0;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var rows = _db.Query<UserRow>(
                "SELECT Id, UserName, Email, NationalId FROM Users");

            var result = new List<User>();
            foreach (var row in rows)
            {
                var user = await MapToDomainAsync(row);
                if (user != null) result.Add(user);
            }
            return result;
        }

        public bool CreateUser(User user)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // Insert base user
                using var insertUser = new SqliteCommand(
                    "INSERT INTO Users (UserName, Email, NationalId) VALUES (@UserName, @Email, @NationalId); SELECT last_insert_rowid();",
                    conn, transaction);
                insertUser.Parameters.AddWithValue("@UserName", user.UserName);
                insertUser.Parameters.AddWithValue("@Email", user.Email);
                insertUser.Parameters.AddWithValue("@NationalId", user.NationalId);
                var newId = Convert.ToInt32(insertUser.ExecuteScalar());
                user.Id = newId;

                InsertRoleData(conn, transaction, user, newId);

                if (user.CurrentVerificationCode != null)
                {
                    using var insertCode = new SqliteCommand(
                        "INSERT INTO VerificationCodes (UserId, Code, CreationDate) VALUES (@UserId, @Code, @CreationDate)",
                        conn, transaction);
                    insertCode.Parameters.AddWithValue("@UserId", newId);
                    insertCode.Parameters.AddWithValue("@Code", user.CurrentVerificationCode.Code);
                    insertCode.Parameters.AddWithValue("@CreationDate", user.CurrentVerificationCode.CreationDate.ToString("o"));
                    insertCode.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public async Task<bool> UpdateAsync(User user)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                using var updateUser = new SqliteCommand(
                    "UPDATE Users SET UserName = @UserName, Email = @Email, NationalId = @NationalId WHERE Id = @Id",
                    conn, transaction);
                updateUser.Parameters.AddWithValue("@UserName", user.UserName);
                updateUser.Parameters.AddWithValue("@Email", user.Email);
                updateUser.Parameters.AddWithValue("@NationalId", user.NationalId);
                updateUser.Parameters.AddWithValue("@Id", user.Id);
                int affected = updateUser.ExecuteNonQuery();
                if (affected == 0) return false;

                UpdateRoleData(conn, transaction, user);
                UpdateVerificationCode(conn, transaction, user);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool DeleteUser(int userId)
        {
            try
            {
                int affected = _db.Execute(
                    "DELETE FROM Users WHERE Id = @Id",
                    new { Id = userId });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        // --- Mapping ---

        private async Task<User?> MapToDomainAsync(UserRow userData)
        {
            User? user = null;

            var isSupervisor = _db.QuerySingle<CountRow>(
                "SELECT COUNT(1) AS Count FROM UserSupervisors WHERE UserId = @UserId",
                new { UserId = userData.Id })?.Count > 0;

            var isAdmin = _db.QuerySingle<CountRow>(
                "SELECT COUNT(1) AS Count FROM UserAdmins WHERE UserId = @UserId",
                new { UserId = userData.Id })?.Count > 0;

            if (isSupervisor)
            {
                user = new Supervisor
                {
                    Id = userData.Id,
                    UserName = userData.UserName,
                    Email = userData.Email,
                    NationalId = userData.NationalId
                };
            }
            else if (isAdmin)
            {
                user = new Admin
                {
                    Id = userData.Id,
                    UserName = userData.UserName,
                    Email = userData.Email,
                    NationalId = userData.NationalId
                };
            }
            else
            {
                var studentRow = _db.QuerySingle<StudentRow>(
                    "SELECT UserId, GradeId FROM UserStudents WHERE UserId = @UserId",
                    new { UserId = userData.Id });

                if (studentRow != null)
                {
                    var grade = await _gradeRepo.GetByIdAsync(studentRow.GradeId)
                                ?? new Grade { Id = 0, Name = "Unknown", Num = 0 };

                    var student = new Student
                    {
                        Id = userData.Id,
                        UserName = userData.UserName,
                        Email = userData.Email,
                        NationalId = userData.NationalId,
                        Grade = grade
                    };

                    var mentorRow = _db.QuerySingle<MentorRow>(
                        "SELECT UserId, SubjectToTeach FROM UserMentors WHERE UserId = @UserId",
                        new { UserId = userData.Id });
                    if (mentorRow != null)
                        student.MentorProfile = new MentorProfile { SubjectToTeach = mentorRow.SubjectToTeach };

                    var menteeRow = _db.QuerySingle<MenteeRow>(
                        "SELECT UserId, SubjectToLearn FROM UserMentees WHERE UserId = @UserId",
                        new { UserId = userData.Id });
                    if (menteeRow != null)
                        student.MenteeProfile = new MenteeProfile { SubjectToLearn = menteeRow.SubjectToLearn };

                    user = student;
                }
            }

            if (user != null)
            {
                var codeRow = _db.QuerySingle<CodeRow>(
                    "SELECT UserId, Code, CreationDate FROM VerificationCodes WHERE UserId = @UserId",
                    new { UserId = userData.Id });

                if (codeRow != null)
                {
                    user.CurrentVerificationCode = new VerificationCode
                    {
                        Code = codeRow.Code,
                        CreationDate = DateTime.Parse(codeRow.CreationDate)
                    };
                }
            }

            return user;
        }

        private static void InsertRoleData(SqliteConnection conn, SqliteTransaction tx, User user, int userId)
        {
            switch (user)
            {
                case Student student:
                    Execute(conn, tx,
                        "INSERT INTO UserStudents (UserId, GradeId) VALUES (@UserId, @GradeId)",
                        ("@UserId", userId), ("@GradeId", student.Grade.Id));

                    if (student.IsMentor)
                        Execute(conn, tx,
                            "INSERT INTO UserMentors (UserId, SubjectToTeach) VALUES (@UserId, @SubjectToTeach)",
                            ("@UserId", userId), ("@SubjectToTeach", student.MentorProfile!.SubjectToTeach));

                    if (student.IsMentee)
                        Execute(conn, tx,
                            "INSERT INTO UserMentees (UserId, SubjectToLearn) VALUES (@UserId, @SubjectToLearn)",
                            ("@UserId", userId), ("@SubjectToLearn", student.MenteeProfile!.SubjectToLearn));
                    break;

                case Supervisor:
                    Execute(conn, tx,
                        "INSERT INTO UserSupervisors (UserId) VALUES (@UserId)",
                        ("@UserId", userId));
                    break;

                case Admin:
                    Execute(conn, tx,
                        "INSERT INTO UserAdmins (UserId) VALUES (@UserId)",
                        ("@UserId", userId));
                    break;
            }
        }

        private void UpdateRoleData(SqliteConnection conn, SqliteTransaction tx, User user)
        {
            if (user is not Student student) return;

            Execute(conn, tx,
                "UPDATE UserStudents SET GradeId = @GradeId WHERE UserId = @UserId",
                ("@GradeId", student.Grade.Id), ("@UserId", student.Id));

            if (student.IsMentor)
            {
                var exists = _db.QuerySingle<CountRow>(
                    "SELECT COUNT(1) AS Count FROM UserMentors WHERE UserId = @UserId",
                    new { UserId = student.Id })?.Count > 0;

                if (exists)
                    Execute(conn, tx,
                        "UPDATE UserMentors SET SubjectToTeach = @SubjectToTeach WHERE UserId = @UserId",
                        ("@SubjectToTeach", student.MentorProfile!.SubjectToTeach), ("@UserId", student.Id));
                else
                    Execute(conn, tx,
                        "INSERT INTO UserMentors (UserId, SubjectToTeach) VALUES (@UserId, @SubjectToTeach)",
                        ("@UserId", student.Id), ("@SubjectToTeach", student.MentorProfile!.SubjectToTeach));
            }
            else
            {
                Execute(conn, tx,
                    "DELETE FROM UserMentors WHERE UserId = @UserId",
                    ("@UserId", student.Id));
            }

            if (student.IsMentee)
            {
                var exists = _db.QuerySingle<CountRow>(
                    "SELECT COUNT(1) AS Count FROM UserMentees WHERE UserId = @UserId",
                    new { UserId = student.Id })?.Count > 0;

                if (exists)
                    Execute(conn, tx,
                        "UPDATE UserMentees SET SubjectToLearn = @SubjectToLearn WHERE UserId = @UserId",
                        ("@SubjectToLearn", student.MenteeProfile!.SubjectToLearn), ("@UserId", student.Id));
                else
                    Execute(conn, tx,
                        "INSERT INTO UserMentees (UserId, SubjectToLearn) VALUES (@UserId, @SubjectToLearn)",
                        ("@UserId", student.Id), ("@SubjectToLearn", student.MenteeProfile!.SubjectToLearn));
            }
            else
            {
                Execute(conn, tx,
                    "DELETE FROM UserMentees WHERE UserId = @UserId",
                    ("@UserId", student.Id));
            }
        }

        private static void UpdateVerificationCode(SqliteConnection conn, SqliteTransaction tx, User user)
        {
            if (user.CurrentVerificationCode != null)
            {
                Execute(conn, tx,
                    @"INSERT INTO VerificationCodes (UserId, Code, CreationDate)
                      VALUES (@UserId, @Code, @CreationDate)
                      ON CONFLICT(UserId) DO UPDATE SET Code = excluded.Code, CreationDate = excluded.CreationDate",
                    ("@UserId", user.Id),
                    ("@Code", user.CurrentVerificationCode.Code),
                    ("@CreationDate", user.CurrentVerificationCode.CreationDate.ToString("o")));
            }
            else
            {
                Execute(conn, tx,
                    "DELETE FROM VerificationCodes WHERE UserId = @UserId",
                    ("@UserId", user.Id));
            }
        }

        private static void Execute(SqliteConnection conn, SqliteTransaction tx, string sql, params (string name, object? value)[] parameters)
        {
            using var cmd = new SqliteCommand(sql, conn, tx);
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // --- Row DTOs ---

        private class UserRow
        {
            public int Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string NationalId { get; set; } = string.Empty;
        }

        private class CountRow
        {
            public int Count { get; set; }
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
    }
}
