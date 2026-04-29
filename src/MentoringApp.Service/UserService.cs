using System;
using System.IO.IsolatedStorage;
using MentoringApp.Data.Dao;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Model.User.StudentProfiles;
using MentoringApp.Service.Mapping;

namespace MentoringApp.Service
{
    /// <summary>
    /// Application-level user service. Owns DTO-to-domain mapping (<see cref="MapDtoToUserAsync"/>)
    /// and all CRUD operations that cross the data layer.
    /// </summary>
    public class UserService
    {
        private readonly IUserRepo _userRepo;
        private readonly IGradeRepo _gradeRepo;
        private readonly IIssueRepo _issueRepo;       
        private readonly IIssueCategoryRepo _issueCategoryRepo;
        private readonly IPairRepo _pairRepo;
        private readonly ISchoolClassRepo _schoolClassRepo;

        public UserService(IUserRepo userRepository, IGradeRepo gradeRepository, IIssueRepo issueRepo, IIssueCategoryRepo issueCategoryRepo, IPairRepo pairRepo, ISchoolClassRepo schoolClassRepo)
        {
            _userRepo = userRepository;
            _gradeRepo = gradeRepository;
            _issueRepo = issueRepo;
            _issueCategoryRepo = issueCategoryRepo;
            _pairRepo = pairRepo;
            _schoolClassRepo = schoolClassRepo;
        }

        public async Task<Result<UserModel>> GetUserByIdAsync(int userId)
        {
            var dto = await _userRepo.GetUserDtoByIdAsync(userId);
            if (dto == null) return Result<UserModel>.Failure("User not found.");

            var user = await MapDtoToUserAsync(dto);
            return Result<UserModel>.Ok(user);
        }

        public async Task<Result<UserModel>> GetUserByNationalIdAsync(string nationalId)
        {
            var dto = await _userRepo.GetUserDtoByNationalIdAsync(nationalId);
            if (dto == null) return Result<UserModel>.Failure("User not found.");

            var user = await MapDtoToUserAsync(dto);
            return Result<UserModel>.Ok(user);
        }
        public async Task<Result> CreateUserAsync(UserModel user)
        {
            bool res = await _userRepo.CreateUserAsync(user);
            if (!res)
            {
                Result.Failure("user failed to creat");
            }
            return Result.Ok();

        }

        public async Task<IEnumerable<UserModel>> GetProblematicSupervisorsAsync(int count)
        {
            var stats = await _userRepo.GetSupervisorStatisticsAsync();

            var problematicStats = stats
                .OrderByDescending(s => s.PendingIssuesCount)
                .Take(count);

            var results = new List<UserModel>();

            foreach (var stat in problematicStats)
            {
                var dto = await _userRepo.GetUserDtoByIdAsync(stat.Id);
                var user = await MapDtoToUserAsync(dto);
                results.Add(user);
            }

            return results;
        }

        /// <summary>
        /// Converts a flat <see cref="UserDao"/> into the appropriate domain model subtype
        /// (<see cref="AdminModel"/>, <see cref="SupervisorModel"/>, or <see cref="StudentModel"/>),
        /// eagerly loading related data (grade, issues, classes) via additional async queries.
        /// </summary>
        private async Task<UserModel> MapDtoToUserAsync(UserDao dto)
        {
            UserModel user;

            switch (dto.Role)
            {
                case UserRoleType.Admin:
                    user = new AdminModel(dto.Id, dto.Email, dto.UserName, dto.NationalId);
                    break;

                case UserRoleType.Supervisor:
                    var supervisor = new SupervisorModel(dto.Id, dto.Email, dto.UserName, dto.NationalId);

                    var issueDtos = await _issueRepo.GetAllAsync();
                    var categoryDto = await _issueCategoryRepo.GetAllAsync();
                    var categorys = IssueCategoryMapper.ToModels(categoryDto);
                    supervisor.Issues = IssueMapper.ToModels(issueDtos, categorys);

                    supervisor.SupervisedPairsCount = (await _pairRepo.GetBySupervisorIdAsync(supervisor.Id)).Count();

                    // Load assigned school classes
                    var classDtos = await _schoolClassRepo.GetBySupervisorAsync(supervisor.Id);
                    var allGrades = (await _gradeRepo.GetAllGradesAsync()).ToDictionary(g => g.Id);
                    supervisor.AssignedClasses = classDtos.Select(dto =>
                    {
                        if (!allGrades.TryGetValue(dto.GradeId, out var gDto)) return null;
                        return new SchoolClassModel
                        {
                            Id = dto.Id,
                            Grade = new GradeModel { Id = gDto.Id, Name = gDto.Name, Num = gDto.Num },
                            ClassNum = dto.ClassNum
                        };
                    }).Where(x => x != null).Cast<SchoolClassModel>().ToList();

                    user = supervisor;
                    break;

                case UserRoleType.Student:
                    var gradeDto = await _gradeRepo.GetByIdAsync(dto.GradeId ?? 0)
                                ?? new GradeDao { Id = 0, Name = "Unknown", Num = 0 };

                    var student = new StudentModel(dto.Id, dto.Email, dto.UserName, dto.NationalId, new GradeModel { Id = gradeDto.Id, Name = gradeDto.Name, Num = gradeDto.Num });
                    student.ClassNum = dto.ClassNum ?? 0;
                    student.PreferredMentorGender = dto.PreferredMentorGender.HasValue
                        ? (MentoringApp.Model.User.GenderPreference)dto.PreferredMentorGender.Value
                        : MentoringApp.Model.User.GenderPreference.NoPreference;
                    student.PreferredMenteeGender = dto.PreferredMenteeGender.HasValue
                        ? (MentoringApp.Model.User.GenderPreference)dto.PreferredMenteeGender.Value
                        : MentoringApp.Model.User.GenderPreference.NoPreference;

                    if (dto.MentorSubjectId.HasValue)
                    {
                        student.MentorProfile = new MentorProfile {
                            SubjectToTeach = dto.MentorSubjectId.Value,
                            MaxMentees = dto.MaxMentees ?? 1
                        };
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

            // Map language preference
            user.Language = dto.Language ?? "en";

            // Map contact and gender info
            user.PhoneNumber = dto.PhoneNumber;
            user.Gender = (MentoringApp.Model.User.Gender)dto.Gender;

            return user;
        }



        /// <summary>
        /// Loads all users and maps them concurrently.
        /// Note: each mapping fires several DB queries, so this can be expensive for large datasets.
        /// </summary>
        public async Task<IEnumerable<UserModel>> GetAllUsersAsync()
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

        public async Task<Result> UpdateUserAsync(UserModel user)
        {
            if (user == null) return Result.Failure("User data is null.");

            bool baseUpdated = await _userRepo.UpdateBaseInfoAsync(
                user.Id, user.UserName, user.Email, user.NationalId, user.PhoneNumber, (int)user.Gender);

            if (!baseUpdated) return Result.Failure("User not found or base update failed.");

            if (user is StudentModel student)
            {
                await _userRepo.UpdateStudentGradeAndClassAsync(student.Id, student.Grade.Id, student.ClassNum);
                await _userRepo.UpdateStudentPreferredGendersAsync(student.Id, (int)student.PreferredMentorGender, (int)student.PreferredMenteeGender);

                if (student.MentorProfile != null)
                {
                    await _userRepo.UpsertMentorProfileAsync(student.Id, student.MentorProfile.SubjectToTeach);
                }
            }
            else if (user is SupervisorModel supervisor)
            {
                // UpdateSupervisorClassesAsync is called separately from ManageUsersViewModel via SchoolClassService
            }
            else if (user is AdminModel)
            {
            }

            return Result.Ok();
        }

        public async Task<Result> UploadProfilePictureAsync(int userId, string sourceFilePath)
        {
            Result<UserModel> result = await GetUserByIdAsync(userId);
            if (!result.Success) return Result.Failure(result.ErrorMessage);
            UserModel user = result.Data;

            if (!user.IsValidProfilePicture(sourceFilePath))
                return Result.Failure("Invalid format");

            MoveFileToLocalStorage(user.Id, sourceFilePath);
            string destPath = MoveFileToLocalStorage(userId, sourceFilePath);

            await _userRepo.UpdateProfilePictureAsync(userId, destPath);
            return Result.Ok();
        }

        public async Task<Result> UpdateLanguageAsync(int userId, string language)
        {
            bool updated = await _userRepo.UpdateLanguageAsync(userId, language);
            return updated ? Result.Ok() : Result.Failure("Failed to update language.");
        }

        /// <summary>
        /// Copies the source file into the app's LocalApplicationData folder,
        /// naming it "{userId}{ext}" (overwriting any previous picture for that user).
        /// Returns the absolute destination path that should be persisted to the DB.
        /// </summary>
        private string MoveFileToLocalStorage(int userId, string sourceFilePath)
        {
            string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();

            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MentoringApp", "ProfilePictures");

            Directory.CreateDirectory(folder);

            string destFileName = $"{userId}{ext}";
            string destPath = Path.Combine(folder, destFileName);

            File.Copy(sourceFilePath, destPath, overwrite: true);

            return destPath;
        }
    }
}

