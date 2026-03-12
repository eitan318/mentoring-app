using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlSubjectRepo : ISubjectRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlSubjectRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            var rows = _db.Query<SubjectRow>("SELECT Id, Name FROM Subjects");
            var result = rows.Select(r => new Subject { Id = r.Id, Name = r.Name });
            return Task.FromResult<IEnumerable<Subject>>(result.ToList());
        }

        private class SubjectRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
