using FluentAssertions;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.Service.Validation;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.User;
using Moq;
using Xunit;

namespace MentoringApp.ViewModel.Tests
{
    /// <summary>
    /// Tests for LoginViewModel. Demonstrates the value of the ViewModel/Service separation:
    /// ViewModels are pure C# (no WPF runtime needed) and every dependency can be
    /// controlled via mocked repo interfaces, so VM state changes are verifiable in isolation.
    /// </summary>
    public class LoginViewModelTests
    {
        // ── Shared factory helpers ─────────────────────────────────────────────

        private static UserService BuildUserService(Mock<IUserRepo> userRepo)
        {
            var gradeRepo = new Mock<IGradeRepo>();
            gradeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((GradeDto?)null);
            gradeRepo.Setup(r => r.GetAllGradesAsync())
                     .ReturnsAsync(Array.Empty<GradeDto>());

            return new UserService(
                userRepo.Object,
                gradeRepo.Object,
                new Mock<IIssueRepo>().Object,
                new Mock<IIssueCategoryRepo>().Object,
                new Mock<IPairRepo>().Object,
                new Mock<ISchoolClassRepo>().Object);
        }

        private static AuthService BuildAuthService(
            Mock<IUserRepo> userRepo,
            Mock<IVerificationCodeRepo> verificationRepo)
        {
            var userService = BuildUserService(userRepo);
            return new AuthService(
                userService,
                verificationRepo.Object,
                new UserValidator(),
                new EmailService("localhost", 25, "test@test.com", "pwd"));
        }

        private static UserDto MakeAdminDto(int id = 1, string nationalId = "123456789") =>
            new UserDto
            {
                Id = id,
                UserName = "Test User",
                Email = "test@test.com",
                NationalId = nationalId,
                Role = UserRoleType.Admin,
                Gender = 3
            };

        private LoginViewModel BuildViewModel(
            Mock<IUserRepo> userRepo,
            Mock<IVerificationCodeRepo> verificationRepo,
            Mock<INavigationService> navService,
            Mock<ILanguageService> languageService,
            UserStore? store = null)
        {
            var authService = BuildAuthService(userRepo, verificationRepo);
            var userService = BuildUserService(userRepo);
            var settingsService = new SettingsService(new Mock<ISettingsRepo>().Object);
            var sessionService = new SessionService();

            return new LoginViewModel(
                store ?? new UserStore(),
                navService.Object,
                authService,
                languageService.Object,
                settingsService,
                userService,
                sessionService);
        }

        // ── OnNavigatedToAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedTo_ResetsLanguageToEnglish()
        {
            var langService = new Mock<ILanguageService>();
            var vm = BuildViewModel(new Mock<IUserRepo>(), new Mock<IVerificationCodeRepo>(),
                new Mock<INavigationService>(), langService);

            vm.SelectedLanguage = "he"; // simulate previously-changed language
            await vm.OnNavigatedToAsync();

            vm.SelectedLanguage.Should().Be("en");
        }

        [Fact]
        public async Task OnNavigatedTo_AppliesEnglishToLanguageService()
        {
            var langService = new Mock<ILanguageService>();
            var vm = BuildViewModel(new Mock<IUserRepo>(), new Mock<IVerificationCodeRepo>(),
                new Mock<INavigationService>(), langService);

            await vm.OnNavigatedToAsync();

            langService.Verify(l => l.ApplyLanguage("en"), Times.AtLeastOnce);
        }

        // ── SendVerificationCode ───────────────────────────────────────────────

        [Fact]
        public async Task SendVerificationCode_SetsErrorMessage_WhenUserDoesNotExist()
        {
            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByNationalIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((UserDto?)null);

            var vm = BuildViewModel(userRepo, new Mock<IVerificationCodeRepo>(),
                new Mock<INavigationService>(), new Mock<ILanguageService>());

            vm.NationalId = "000000000";
            await vm.SendVerificationCodeCommand.ExecuteAsync(null);

            vm.WasCodeSent.Should().BeFalse();
            vm.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SendVerificationCode_LeavesWasCodeSentFalse_OnAnyFailure()
        {
            // Any failure path (user not found, repo error, etc.) must NOT flip WasCodeSent.
            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByNationalIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((UserDto?)null);

            var vm = BuildViewModel(userRepo, new Mock<IVerificationCodeRepo>(),
                new Mock<INavigationService>(), new Mock<ILanguageService>());

            vm.NationalId = "999999999";
            await vm.SendVerificationCodeCommand.ExecuteAsync(null);

            vm.WasCodeSent.Should().BeFalse();
        }

        // ── Login ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_SetsErrorMessage_WhenVerificationCodeIsInvalid()
        {
            var verificationRepo = new Mock<IVerificationCodeRepo>();
            verificationRepo.Setup(r => r.GetUserIdByCodeAsync(It.IsAny<string>()))
                            .ReturnsAsync((int?)null); // code not found

            var vm = BuildViewModel(new Mock<IUserRepo>(), verificationRepo,
                new Mock<INavigationService>(), new Mock<ILanguageService>());

            vm.NationalId = "123456789";
            vm.VerificationCode = "BADCODE";
            await vm.LoginCommand.ExecuteAsync(null);

            vm.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_SetsErrorMessage_WhenCodeIsExpired()
        {
            const int userId = 5;
            var dto = MakeAdminDto(id: userId);
            dto.VerificationCode = "OLDCODE";
            dto.VerificationCodeCreated = DateTime.Now.AddMinutes(-20); // expired

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(userId)).ReturnsAsync(dto);

            var verificationRepo = new Mock<IVerificationCodeRepo>();
            verificationRepo.Setup(r => r.GetUserIdByCodeAsync("OLDCODE")).ReturnsAsync(userId);

            var vm = BuildViewModel(userRepo, verificationRepo,
                new Mock<INavigationService>(), new Mock<ILanguageService>());

            vm.NationalId = dto.NationalId;
            vm.VerificationCode = "OLDCODE";
            await vm.LoginCommand.ExecuteAsync(null);

            vm.ErrorMessage.Should().Contain("expired");
        }

        [Fact]
        public async Task Login_PopulatesUserStore_WhenCodeAndCredentialsValid()
        {
            const int userId = 7;
            var dto = MakeAdminDto(id: userId, nationalId: "123456789");
            dto.VerificationCode = "VALID1";
            dto.VerificationCodeCreated = DateTime.Now.AddMinutes(-3);

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(userId)).ReturnsAsync(dto);
            userRepo.Setup(r => r.GetUserDtoByNationalIdAsync("123456789")).ReturnsAsync(dto);

            var verificationRepo = new Mock<IVerificationCodeRepo>();
            verificationRepo.Setup(r => r.GetUserIdByCodeAsync("VALID1")).ReturnsAsync(userId);
            verificationRepo.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(true);

            var navService = new Mock<INavigationService>();
            var store = new UserStore();

            var vm = BuildViewModel(userRepo, verificationRepo, navService,
                new Mock<ILanguageService>(), store);

            vm.NationalId = "123456789";
            vm.VerificationCode = "VALID1";
            await vm.LoginCommand.ExecuteAsync(null);

            store.User.Should().NotBeNull();
            store.User!.Id.Should().Be(userId);
        }

        [Fact]
        public async Task Login_TriggersNavigation_WhenCodeAndCredentialsValid()
        {
            const int userId = 8;
            var dto = MakeAdminDto(id: userId, nationalId: "123456789");
            dto.VerificationCode = "VALID2";
            dto.VerificationCodeCreated = DateTime.Now.AddMinutes(-1);

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(userId)).ReturnsAsync(dto);
            userRepo.Setup(r => r.GetUserDtoByNationalIdAsync("123456789")).ReturnsAsync(dto);

            var verificationRepo = new Mock<IVerificationCodeRepo>();
            verificationRepo.Setup(r => r.GetUserIdByCodeAsync("VALID2")).ReturnsAsync(userId);
            verificationRepo.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(true);

            var navService = new Mock<INavigationService>();

            var vm = BuildViewModel(userRepo, verificationRepo, navService, new Mock<ILanguageService>());

            vm.NationalId = "123456789";
            vm.VerificationCode = "VALID2";
            await vm.LoginCommand.ExecuteAsync(null);

            // INavigationService.NavigateToAsync<T>() must have been called exactly once
            navService.Verify(
                n => n.NavigateToAsync<MentoringApp.ViewModel.ViewModel.User.AuthenticatedDashboardViewModel>(),
                Times.Once);
        }
    }
}
