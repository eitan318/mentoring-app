using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using Microsoft.EntityFrameworkCore;
using MentoringApp.Data.SQLEF.DataObject;

namespace MentoringApp.Data.SQLEF
{
    internal class EFUserRepo : IUserRepo
    {
        private readonly MentoringDbContext _context;

        public EFUserRepo(MentoringDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public User? LoadUserByNationalId(string nationalId)
        {
            var userData = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.NationalId == nationalId);

            return userData == null ? null : MapToDomain(userData);
        }

        public User? LoadUserById(int userId)
        {
            var userData = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId);

            return userData == null ? null : MapToDomain(userData);
        }

        public bool UserExists(string nationalId)
        {
            return _context.Users.Any(u => u.NationalId == nationalId);
        }

        public List<User> GetAllUsers()
        {
            return _context.Users
                .AsNoTracking()
                .Select(u => MapToDomain(u))
                .Where(u => u != null)
                .ToList()!;
        }

        public bool CreateUser(User user)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var userData = new UserData
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    NationalId = user.NationalId
                };

                _context.Users.Add(userData);
                _context.SaveChanges();

                user.Id = userData.Id;
                CreateRoleData(user, userData.Id);

                if (user.CurrentVerificationCode != null)
                {
                    _context.VerificationCodes.Add(new VerificationCodeData
                    {
                        UserId = user.Id,
                        Code = user.CurrentVerificationCode.Code,
                        CreationDate = user.CurrentVerificationCode.CreationDate
                    });
                }

                _context.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public async Task<bool> UpdateAsync(User user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
                if (existingUser == null) return false;

                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.NationalId = user.NationalId;

                await UpdateRoleDataAsync(user);

                // Update Verification Code
                var existingCode = await _context.VerificationCodes.FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (user.CurrentVerificationCode != null)
                {
                    if (existingCode == null)
                    {
                        _context.VerificationCodes.Add(new VerificationCodeData
                        {
                            UserId = user.Id,
                            Code = user.CurrentVerificationCode.Code,
                            CreationDate = user.CurrentVerificationCode.CreationDate
                        });
                    }
                    else
                    {
                        existingCode.Code = user.CurrentVerificationCode.Code;
                        existingCode.CreationDate = user.CurrentVerificationCode.CreationDate;
                    }
                }
                else if (existingCode != null)
                {
                    _context.VerificationCodes.Remove(existingCode);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // --- Mapping Logic ---

        private User? MapToDomain(UserData userData)
        {
            User user = null;

            if (_context.Supervisors.Any(s => s.UserId == userData.Id))
            {
                user = new Supervisor
                {
                    Id = userData.Id,
                    UserName = userData.UserName,
                    Email = userData.Email,
                    NationalId = userData.NationalId
                };
            }
            else if (_context.Admins.Any(a => a.UserId == userData.Id))
            {
                user = new Admin
                {
                    Id = userData.Id,
                    UserName = userData.UserName,
                    Email = userData.Email,
                    NationalId = userData.NationalId
                };
            }
            else
            {
                var studentData = _context.Students.FirstOrDefault(s => s.UserId == userData.Id);
                if (studentData != null)
                {
                    var student = new Student
                    {
                        Id = userData.Id,
                        UserName = userData.UserName,
                        Email = userData.Email,
                        NationalId = userData.NationalId,
                        Grade = studentData.Grade
                    };

                    var mentorData = _context.Mentors.FirstOrDefault(m => m.UserId == userData.Id);
                    if (mentorData != null)
                    {
                        student.MentorProfile = new MentorProfile { SubjectToTeach = mentorData.SubjectToTeach };
                    }

                    var menteeData = _context.Mentees.FirstOrDefault(m => m.UserId == userData.Id);
                    if (menteeData != null)
                    {
                        student.MenteeProfile = new MenteeProfile { SubjectToLearn = menteeData.SubjectToLearn };
                    }

                    user = student;
                }
            }

            if (user != null)
            {
                var codeData = _context.VerificationCodes.FirstOrDefault(c => c.UserId == userData.Id);
                if (codeData != null)
                {
                    user.CurrentVerificationCode = new VerificationCode
                    {
                        Code = codeData.Code,
                        CreationDate = codeData.CreationDate
                    };
                }
            }

            return user;
        }

        private void CreateRoleData(User user, int userId)
        {
            switch (user)
            {
                case Student student:
                    _context.Students.Add(new UserStudentData { UserId = userId, Grade = student.Grade });

                    if (student.IsMentor)
                        _context.Mentors.Add(new UserMentorData { UserId = userId, SubjectToTeach = student.MentorProfile.SubjectToTeach });

                    if (student.IsMentee)
                        _context.Mentees.Add(new UserMenteeData { UserId = userId, SubjectToLearn = student.MenteeProfile.SubjectToLearn });
                    break;

                case Supervisor:
                    _context.Supervisors.Add(new UserSupervisorData { UserId = userId });
                    break;

                case Admin:
                    _context.Admins.Add(new UserAdminData { UserId = userId });
                    break;
            }
        }

        private async Task UpdateRoleDataAsync(User user)
        {
            if (user is Student student)
            {
                var studentData = await _context.Students.FirstOrDefaultAsync(s => s.UserId == student.Id);
                if (studentData != null) studentData.Grade = student.Grade;

                // Mentor
                var mentorData = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == student.Id);
                if (student.IsMentor)
                {
                    if (mentorData == null)
                        _context.Mentors.Add(new UserMentorData { UserId = student.Id, SubjectToTeach = student.MentorProfile.SubjectToTeach });
                    else
                        mentorData.SubjectToTeach = student.MentorProfile.SubjectToTeach;
                }
                else if (mentorData != null)
                {
                    _context.Mentors.Remove(mentorData);
                }

                // Mentee
                var menteeData = await _context.Mentees.FirstOrDefaultAsync(m => m.UserId == student.Id);
                if (student.IsMentee)
                {
                    if (menteeData == null)
                        _context.Mentees.Add(new UserMenteeData { UserId = student.Id, SubjectToLearn = student.MenteeProfile.SubjectToLearn });
                    else
                        menteeData.SubjectToLearn = student.MenteeProfile.SubjectToLearn;
                }
                else if (menteeData != null)
                {
                    _context.Mentees.Remove(menteeData);
                }
            }
        }
    }
}