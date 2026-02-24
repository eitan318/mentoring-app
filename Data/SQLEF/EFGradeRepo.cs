using MentoringApp.Data.Interfaces;
using MentoringApp.Data.SQLEF.DataObject;
using MentoringApp.Model;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Data.SQLEF
{
    internal class EFGradeRepo : IGradeRepo
    {
        private readonly MentoringDbContext _context;

        public EFGradeRepo(MentoringDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Grade?> GetByIdAsync(int id)
        {
            var data = await _context.Grades
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (data == null) return null;

            return MapToDomain(data);
        }

        public async Task<IEnumerable<Grade>> GetAllGradesAsync()
        {
            // Fetch data from DB first to avoid LINQ translation issues with custom mapping
            var data = await _context.Grades
                .AsNoTracking()
                .ToListAsync();

            return data.Select(MapToDomain);
        }

        private static Grade MapToDomain(GradeData data)
        {
            return new Grade
            {
                Id = data.Id,
                Name = data.Name,
                // Converting string from DB to int for the Domain model
                Num = int.TryParse(data.Num, out int result) ? result : 0
            };
        }
    }
}