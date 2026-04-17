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

        public async Task<Result<IEnumerable<Subject>>> GetAllSubjectsAsync()
        {
            var dtos = await _subjectRepo.GetAllSubjectsAsync();
            var subjects = dtos.Select(MapDtoToSubject);
            return Result<IEnumerable<Subject>>.Ok(subjects);
        }

        private static Subject MapDtoToSubject(SubjectDto dto) =>
            new Subject { Id = dto.Id, Name = dto.Name };
    }
}