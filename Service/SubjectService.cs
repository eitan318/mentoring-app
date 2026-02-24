using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Service
{
    public class SubjectService
    {
        private readonly ISubjectRepo _subjectRepo;

        public SubjectService(ISubjectRepo reviewRepo)
        {
            _subjectRepo = reviewRepo;
        }

        public async Task<Result<IEnumerable<Subject>>> GetAllSubjectsAsync()
        {
            var subjects = await _subjectRepo.GetAllSubjectsAsync();
            return Result<IEnumerable<Subject>>.Ok(subjects);
        }

    }

}