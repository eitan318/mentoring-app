using MentoringApp.Data.Interfaces;
using MentoringApp.Data.SQLEF.DataObject;
using MentoringApp.Model;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Data.SQLEF
{
    internal class EFPairRepo : IPairRepo
    {
        private readonly MentoringDbContext _context;

        private readonly IGradeRepo _gradeRepo;

        public EFPairRepo(MentoringDbContext context, IGradeRepo gradeRepo)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Pair?> GetByMentorIdAsync(int mentorId)
        {
            var data = await _context.Pairs
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MentorId == mentorId);
            return data == null ? null : MapToDomain(data);
        }

        public Pair? GetById(int id)
        {
            var data = _context.Pairs.AsNoTracking().FirstOrDefault(p => p.Id == id);
            return data == null ? null : MapToDomain(data);
        }


        public async Task<IEnumerable<Pair>> GetAllAsync()
        {
            // Fetch from DB async first
            var data = await _context.Pairs
                .AsNoTracking()
                .ToListAsync();

            // Then map in memory
            return data
                .Select(p => MapToDomain(p))
                .Where(p => p != null)!;
        }

        public IEnumerable<Pair> GetBySupervisorId(int supervisorId)
        {
            return _context.Pairs
                .AsNoTracking()
                .Where(p => p.SupervisorId == supervisorId)
                .Select(p => MapToDomain(p))
                .Where(p => p != null)
                .ToList()!;
        }

        public async Task<bool> CreateAsync(Pair pair, int supervisorId, int mentorId, int menteeId)
        {
            try
            {
                _context.Pairs.Add(new PairData
                {
                    MentorId = mentorId,
                    MenteeId = menteeId,
                    SupervisorId = supervisorId,
                    CreatedAt = DateTime.UtcNow
                });
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Delete(int pairId)
        {
            var data = _context.Pairs.FirstOrDefault(p => p.Id == pairId);
            if (data == null) return false;

            try
            {
                _context.Pairs.Remove(data);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }


        private Pair? MapToDomain(PairData data)
        {
            var mentor = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == data.MentorId);
            var mentee = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == data.MenteeId);

            

            if (mentor == null || mentee == null) return null;


            return new Pair
            {
                Id = data.Id,
                Mentor = new Student
                {
                    Id = mentor.Id,
                    UserName = mentor.UserName,
                    Email = mentor.Email,
                    NationalId = mentor.NationalId,
                    Grade = _gradeRepo.GetByIdAsync(0).Result,
                    MentorProfile = new MentorProfile()
                },
                Mentee = new Student
                {
                    Id = mentee.Id,
                    UserName = mentee.UserName,
                    Email = mentee.Email,
                    NationalId = mentee.NationalId,
                    Grade = _gradeRepo.GetByIdAsync(9).Result,
                    MenteeProfile = new MenteeProfile()
                }
            };
        }
    }
}
