using MentoringApp.Data.Interfaces;
using MentoringApp.Data.SQLEF.DataObject;
using MentoringApp.Model;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Data.SQLEF
{
    internal class EFSubjectRepo : ISubjectRepo
    {
        private readonly MentoringDbContext _context;

        public EFSubjectRepo(MentoringDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            return _context.Subjects
                .AsNoTracking()
                .Select(r => MapToDomain(r))
                .ToList();
        }

        private static Subject MapToDomain(SubjectData data)
        {
            return new Subject { Name = data.Name, Id = data.Id };  
        }
    }
}
