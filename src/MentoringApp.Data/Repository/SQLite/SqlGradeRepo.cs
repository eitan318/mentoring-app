using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;
using MentoringApp.Data.Dao.User;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlGradeRepo : IGradeRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlGradeRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }
        public async Task<GradeDao?> GetByIdAsync(int id)
        {
            return await _db.QuerySingleAsync<GradeDao>(
                "SELECT Id, Name, Num FROM Grades WHERE Id = @Id",
                new { Id = id });
        }

        public async Task<IEnumerable<GradeDao>> GetAllGradesAsync()
        {
            return await _db.QueryAsync<GradeDao>("SELECT Id, Name, Num FROM Grades");
        }

        private class GradeRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Num { get; set; } = string.Empty;
        }
    }
}
