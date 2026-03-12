using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlPairRepo : IPairRepo
    {
        private readonly ISQLiteConnectionService _db;
        private readonly IGradeRepo _gradeRepo;

        public SqlPairRepo(ISQLiteConnectionService db, IGradeRepo gradeRepo)
        {
            _db = db;
            _gradeRepo = gradeRepo;
        }

        public async Task<IEnumerable<Pair>> GetAllAsync()
        {
            var rows = _db.Query<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs");

            var result = new List<Pair>();
            foreach (var row in rows)
            {
                var pair = await MapToDomainAsync(row);
                if (pair != null) result.Add(pair);
            }
            return result;
        }

        public Pair? GetById(int id)
        {
            var row = _db.QuerySingle<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE Id = @Id",
                new { Id = id });
            return row == null ? null : MapToDomainAsync(row).GetAwaiter().GetResult();
        }

        public async Task<Pair?> GetByMentorIdAsync(int mentorId)
        {
            var row = _db.QuerySingle<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE MentorId = @MentorId",
                new { MentorId = mentorId });
            return row == null ? null : await MapToDomainAsync(row);
        }

        public async Task<Pair?> GetByMenteeIdAsync(int menteeId)
        {
            var row = _db.QuerySingle<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE MenteeId = @MenteeId",
                new { MenteeId = menteeId });
            return row == null ? null : await MapToDomainAsync(row);
        }

        public IEnumerable<Pair> GetBySupervisorId(int supervisorId)
        {
            var rows = _db.Query<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE SupervisorId = @SupervisorId",
                new { SupervisorId = supervisorId });

            var result = new List<Pair>();
            foreach (var row in rows)
            {
                var pair = MapToDomainAsync(row).GetAwaiter().GetResult();
                if (pair != null) result.Add(pair);
            }
            return result;
        }

        public Task<bool> CreateAsync(Pair pair, int supervisorId, int mentorId, int menteeId)
        {
            try
            {
                _db.Execute(
                    @"INSERT INTO Pairs (MentorId, MenteeId, SupervisorId, CreatedAt)
                      VALUES (@MentorId, @MenteeId, @SupervisorId, @CreatedAt)",
                    new
                    {
                        MentorId = mentorId,
                        MenteeId = menteeId,
                        SupervisorId = supervisorId,
                        CreatedAt = DateTime.UtcNow.ToString("o")
                    });
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public bool Delete(int pairId)
        {
            try
            {
                int affected = _db.Execute(
                    "DELETE FROM Pairs WHERE Id = @Id",
                    new { Id = pairId });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<Pair?> MapToDomainAsync(PairRow row)
        {
            var mentorUser = LoadUserRow(row.MentorId);
            var menteeUser = LoadUserRow(row.MenteeId);

            if (mentorUser == null || menteeUser == null) return null;

            var mentorStudent = await BuildStudentAsync(mentorUser);
            var menteeStudent = await BuildStudentAsync(menteeUser);

            if (mentorStudent == null || menteeStudent == null) return null;

            return new Pair
            {
                Id = row.Id,
                Mentor = mentorStudent,
                Mentee = menteeStudent
            };
        }

        private UserRow? LoadUserRow(int userId)
        {
            return _db.QuerySingle<UserRow>(
                "SELECT Id, UserName, Email, NationalId FROM Users WHERE Id = @Id",
                new { Id = userId });
        }

        private async Task<Student?> BuildStudentAsync(UserRow userRow)
        {
            var studentRow = _db.QuerySingle<StudentRow>(
                "SELECT UserId, GradeId FROM UserStudents WHERE UserId = @UserId",
                new { UserId = userRow.Id });

            if (studentRow == null) return null;

            var grade = await _gradeRepo.GetByIdAsync(studentRow.GradeId) ?? new Grade { Id = 0, Name = "Unknown", Num = 0 };

            var student = new Student
            {
                Id = userRow.Id,
                UserName = userRow.UserName,
                Email = userRow.Email,
                NationalId = userRow.NationalId,
                Grade = grade
            };

            var mentorRow = _db.QuerySingle<MentorRow>(
                "SELECT UserId, SubjectToTeach FROM UserMentors WHERE UserId = @UserId",
                new { UserId = userRow.Id });
            if (mentorRow != null)
                student.MentorProfile = new MentorProfile { SubjectToTeach = mentorRow.SubjectToTeach };

            var menteeRow = _db.QuerySingle<MenteeRow>(
                "SELECT UserId, SubjectToLearn FROM UserMentees WHERE UserId = @UserId",
                new { UserId = userRow.Id });
            if (menteeRow != null)
                student.MenteeProfile = new MenteeProfile { SubjectToLearn = menteeRow.SubjectToLearn };

            return student;
        }

        private class PairRow
        {
            public int Id { get; set; }
            public int MentorId { get; set; }
            public int MenteeId { get; set; }
            public int SupervisorId { get; set; }
            public string CreatedAt { get; set; } = string.Empty;
        }

        private class UserRow
        {
            public int Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string NationalId { get; set; } = string.Empty;
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
    }
}
