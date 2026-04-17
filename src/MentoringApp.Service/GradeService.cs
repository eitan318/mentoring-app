using MentoringApp.Data.DTO;
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
                IEnumerable<GradeDto> dtos = await _gradeRepo.GetAllGradesAsync();

                if (dtos == null || !dtos.Any())
                {
                    return Result<IEnumerable<Grade>>.Failure("No grades found.");
                }

                var grades = dtos.Select(dto => new Grade
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Num = dto.Num
                });

                return Result<IEnumerable<Grade>>.Ok(grades);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Grade>>.Failure($"Failed to load grades: {ex.Message}");
            }
        }
    }
}