using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;
using MentoringApp.Data.DTO;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlGradeRepo : IGradeRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlGradeRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }
        public async Task<GradeDto?> GetByIdAsync(int id)
        {
            return await _db.QuerySingleAsync<GradeDto>(
                "SELECT Id, Name, Num FROM Grades WHERE Id = @Id",
                new { Id = id });
        }

        public async Task<IEnumerable<GradeDto>> GetAllGradesAsync()
        {
            return await _db.QueryAsync<GradeDto>("SELECT Id, Name, Num FROM Grades");
        }

        private class GradeRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Num { get; set; } = string.Empty;
        }
    }
}
