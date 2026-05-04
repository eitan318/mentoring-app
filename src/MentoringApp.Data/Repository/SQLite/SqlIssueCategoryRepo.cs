using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.Dao;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;

namespace MentoringApp.Data.Acess.SQLite
{
    class SqlIssueCategoryRepo : IIssueCategoryRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlIssueCategoryRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public async Task<IEnumerable<IssueCategoryDao>> GetAllAsync()
        {
            var rows = await _db.QueryAsync<CategoryRow>("SELECT Id, Name FROM IssueCategories");
            return rows.Select(r => new IssueCategoryDao { Id = r.Id, Name = r.Name }).ToList();
        }

        public async Task<IssueCategoryDao?> GetByIdAsync(int categoryId)
        {
            var row = await _db.QuerySingleAsync<CategoryRow>(
                "SELECT Id, Name FROM IssueCategories WHERE Id = @Id",
                new { Id = categoryId });
            return row == null ? null : new IssueCategoryDao { Id = row.Id, Name = row.Name };
        }


        private class CategoryRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
