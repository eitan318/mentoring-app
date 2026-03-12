using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlGradeRepo : IGradeRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlGradeRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public Task<Grade?> GetByIdAsync(int id)
        {
            var row = _db.QuerySingle<GradeRow>(
                "SELECT Id, Name, Num FROM Grades WHERE Id = @Id",
                new { Id = id });

            return Task.FromResult(row == null ? null : MapToDomain(row));
        }

        public Task<IEnumerable<Grade>> GetAllGradesAsync()
        {
            var rows = _db.Query<GradeRow>("SELECT Id, Name, Num FROM Grades");
            return Task.FromResult<IEnumerable<Grade>>(rows.Select(MapToDomain).ToList());
        }

        private static Grade MapToDomain(GradeRow row) => new Grade
        {
            Id = row.Id,
            Name = row.Name,
            Num = int.TryParse(row.Num, out int n) ? n : 0
        };

        private class GradeRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Num { get; set; } = string.Empty;
        }
    }
}
