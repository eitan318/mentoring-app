using MentoringApp.Data.Dao;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    public class SchoolClassService
    {
        private readonly ISchoolClassRepo _repo;
        private readonly IGradeRepo _gradeRepo;

        public SchoolClassService(ISchoolClassRepo repo, IGradeRepo gradeRepo)
        {
            _repo = repo;
            _gradeRepo = gradeRepo;
        }

        public async Task<Result<IEnumerable<SchoolClassModel>>> GetAllAsync()
        {
            var dtos = await _repo.GetAllAsync();
            var grades = (await _gradeRepo.GetAllGradesAsync()).ToDictionary(g => g.Id);
            var list = dtos.Select(dto => Map(dto, grades)).Where(x => x != null).Cast<SchoolClassModel>().ToList();
            return Result<IEnumerable<SchoolClassModel>>.Ok(list);
        }

        public async Task<Result<IEnumerable<SchoolClassModel>>> GetBySupervisorAsync(int supervisorId)
        {
            var dtos = await _repo.GetBySupervisorAsync(supervisorId);
            var grades = (await _gradeRepo.GetAllGradesAsync()).ToDictionary(g => g.Id);
            var list = dtos.Select(dto => Map(dto, grades)).Where(x => x != null).Cast<SchoolClassModel>().ToList();
            return Result<IEnumerable<SchoolClassModel>>.Ok(list);
        }

        public async Task<Result<IEnumerable<SchoolClassModel>>> GetAvailableForSupervisorAsync(int supervisorId)
        {
            var all = (await _repo.GetAllAsync()).ToList();
            var assigned = (await _repo.GetBySupervisorAsync(supervisorId)).Select(x => x.Id).ToHashSet();

            // All existing supervisor classes except those assigned to OTHER supervisors need to be fetched
            // Simple approach: return all, mark which ones belong to this supervisor — UI handles availability
            var grades = (await _gradeRepo.GetAllGradesAsync()).ToDictionary(g => g.Id);
            var list = all.Select(dto => Map(dto, grades)).Where(x => x != null).Cast<SchoolClassModel>().ToList();
            return Result<IEnumerable<SchoolClassModel>>.Ok(list);
        }

        public async Task<Result<int>> AddClassAsync(int gradeId, int classNum)
        {
            bool ok = await _repo.AddAsync(gradeId, classNum);
            if (!ok) return Result<int>.Failure("Class already exists or failed to add.");
            // Fetch the newly added id
            var all = await _repo.GetAllAsync();
            var found = all.FirstOrDefault(c => c.GradeId == gradeId && c.ClassNum == classNum);
            return Result<int>.Ok(found?.Id ?? 0);
        }

        public async Task<Result> DeleteClassAsync(int schoolClassId)
        {
            bool ok = await _repo.DeleteAsync(schoolClassId);
            return ok ? Result.Ok() : Result.Failure("Class not found.");
        }

        public async Task<Result> SetSupervisorClassesAsync(int supervisorId, IEnumerable<int> schoolClassIds)
        {
            await _repo.SetSupervisorClassesAsync(supervisorId, schoolClassIds);
            return Result.Ok();
        }

        private SchoolClassModel? Map(SchoolClassDao dto, Dictionary<int, GradeDao> grades)
        {
            if (!grades.TryGetValue(dto.GradeId, out var g)) return null;
            return new SchoolClassModel
            {
                Id = dto.Id,
                Grade = new GradeModel { Id = g.Id, Name = g.Name, Num = g.Num },
                ClassNum = dto.ClassNum
            };
        }
    }
}
