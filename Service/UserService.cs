using System;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    public class UserService
    {
        private readonly IUserRepo _userRepo;
        private readonly IGradeRepo _gradeRepo;

        public UserService(IUserRepo userRepository, IGradeRepo gradeRepository)
        {
            _userRepo = userRepository;
            _gradeRepo = gradeRepository;
        }

        public async Task<Result<User>> GetUserByIdAsync(int userId)
        {
            var dto = await _userRepo.GetUserDtoByIdAsync(userId);
            if (dto == null) return Result<User>.Failure("User not found.");

            var user = await MapDtoToUserAsync(dto);
            return Result<User>.Ok(user);
        }

        public async Task<Result<User>> GetUserByNationalIdAsync(string nationalId)
        {
            var dto = await _userRepo.GetUserDtoByNationalIdAsync(nationalId);
            if (dto == null) return Result<User>.Failure("User not found.");

            var user = await MapDtoToUserAsync(dto);
            return Result<User>.Ok(user);
        }
        public async Task<Result> CreateUserAsync(User user)
        {
            bool res = await _userRepo.CreateUserAsync(user);
            if (!res)
            {
                Result.Failure("user failed to creat");
            }
            return Result.Ok();

        }

        private async Task<User> MapDtoToUserAsync(UserDto dto)
        {
            User user;

            switch (dto.Role)
            {
                case UserRoleType.Admin:
                    user = new Admin(dto.Id, dto.Email, dto.UserName, dto.NationalId);
                    break;

                case UserRoleType.Supervisor:
                    user = new Supervisor(dto.Id, dto.Email, dto.UserName, dto.NationalId);
                    break;

                case UserRoleType.Student:
                    var gradeDto = await _gradeRepo.GetByIdAsync(dto.GradeId ?? 0)
                                ?? new GradeDto { Id = 0, Name = "Unknown", Num = 0 };

                    var student = new Student(dto.Id, dto.Email, dto.UserName, dto.NationalId, new Grade { Id = gradeDto.Id, Name = gradeDto.Name, Num = gradeDto.Num });

                    if (dto.MentorSubjectId.HasValue)
                    {
                        student.MentorProfile = new MentorProfile { SubjectToTeach = dto.MentorSubjectId.Value };
                    }


                    if (dto.MenteeSubjectId.HasValue)
                    {
                        student.MenteeProfile = new MenteeProfile { SubjectToLearn = dto.MenteeSubjectId.Value };
                    }

                    user = student;
                    break;

                default:
                    throw new Exception("Invalid user role.");
            }

            // Map verification data
            if (!string.IsNullOrEmpty(dto.VerificationCode))
            {
                user.CurrentVerificationCode = new VerificationCode
                {
                    Code = dto.VerificationCode,
                    CreationDate = dto.VerificationCodeCreated ?? DateTime.Now
                };
            }

            // Map profile picture
            user.ProfilePicturePath = dto.ProfilePicturePath;

            return user;
        }
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var dtos = await _userRepo.GetAllUserDtosAsync();
            var tasks = dtos.Select(dto => MapDtoToUserAsync(dto));
            return await Task.WhenAll(tasks);
        }

        public async Task<Result> DeleteUserAsync(int userId)
        {

            bool deleted = await _userRepo.DeleteUserAsync(userId);

            return deleted
                ? Result.Ok()
                : Result.Failure("Failed to delete the user from the database.");
        }

        public async Task<Result> UpdateUserAsync(User user)
        {
            if (user == null) return Result.Failure("User data is null.");

            bool baseUpdated = await _userRepo.UpdateBaseInfoAsync(
                user.Id, user.UserName, user.Email, user.NationalId);

            if (!baseUpdated) return Result.Failure("User not found or base update failed.");

            if (user is Student student)
            {
                await _userRepo.UpdateStudentGradeAsync(student.Id, student.Grade.Id);

                if (student.MentorProfile != null)
                {
                    await _userRepo.UpsertMentorProfileAsync(student.Id, student.MentorProfile.SubjectToTeach);
                }
            }
            else if (user is Admin)
            {
            }

            return Result.Ok();
        }

        public async Task<Result> UploadProfilePictureAsync(int userId, string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath))
                return Result.Failure("Selected file does not exist.");

            string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                return Result.Failure("Only .jpg and .png files are supported.");

            // Store pictures in the app's local data folder
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MentoringApp", "ProfilePictures");
            Directory.CreateDirectory(folder);

            string destFileName = $"{userId}{ext}";
            string destPath = Path.Combine(folder, destFileName);

            File.Copy(sourceFilePath, destPath, overwrite: true);

            bool saved = await _userRepo.UpdateProfilePictureAsync(userId, destPath);
            return saved ? Result.Ok() : Result.Failure("Failed to save profile picture path.");
        }
    }
}

