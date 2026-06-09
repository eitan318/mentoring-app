using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    /// <summary>Maps <see cref="SubjectDao"/> DTOs to <see cref="SubjectModel"/> domain objects.</summary>
    public class SubjectService
    {
        private readonly ISubjectRepo _subjectRepo;

        public SubjectService(ISubjectRepo subjectRepo)
        {
            _subjectRepo = subjectRepo;
        }

        public async Task<Result<IEnumerable<SubjectModel>>> GetAllSubjectsAsync()
        {
            var dtos = await _subjectRepo.GetAllSubjectsAsync();
            var subjects = dtos.Select(MapDtoToSubject);
            return Result<IEnumerable<SubjectModel>>.Ok(subjects);
        }

        private static SubjectModel MapDtoToSubject(SubjectDao dto) =>
            new SubjectModel { Id = dto.Id, Name = dto.Name };
    }
}