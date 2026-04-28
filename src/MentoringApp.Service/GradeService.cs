using MentoringApp.Data.Dao;
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

        public async Task<Result<IEnumerable<GradeModel>>> GetAllGradesAsync()
        {
            try
            {
                IEnumerable<GradeDao> dtos = await _gradeRepo.GetAllGradesAsync();

                if (dtos == null || !dtos.Any())
                {
                    return Result<IEnumerable<GradeModel>>.Failure("No grades found.");
                }

                var grades = dtos.Select(dto => new GradeModel
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Num = dto.Num
                });

                return Result<IEnumerable<GradeModel>>.Ok(grades);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<GradeModel>>.Failure($"Failed to load grades: {ex.Message}");
            }
        }
    }
}