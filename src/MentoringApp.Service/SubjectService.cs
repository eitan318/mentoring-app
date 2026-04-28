using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
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