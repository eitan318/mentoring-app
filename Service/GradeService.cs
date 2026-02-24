using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MentoringApp.Service
{
    public class GradeService
    {
        private readonly IGradeRepo _gradeRepo;

        public GradeService(IGradeRepo gradeRepo)
        {
            _gradeRepo = gradeRepo;
        }

        public async Task<Result<IEnumerable<Grade>>> GetAllGradesAsync()
        {
            try
            {
                var grades = await _gradeRepo.GetAllGradesAsync();

                if (grades == null)
                {
                    return Result<IEnumerable<Grade>>.Failure("No grades found.");
                }

                return Result<IEnumerable<Grade>>.Ok(grades);
            }
            catch (Exception ex)
            {
                // You can log the error here if you have a logger
                return Result<IEnumerable<Grade>>.Failure($"Failed to load grades: {ex.Message}");
            }
        }
    }
}