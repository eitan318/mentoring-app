using FluentAssertions;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ViewModel.Auth;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.User;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MentoringApp.ViewModel.Tests
{
    public class LoginViewModelTests
    {
        // ── Helpers ────────────────────────────────────────────────────────────

        private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(handler(request));
        }

        private static string MakeJwt(int userId, string role = "Admin", string language = "en")
        {
            var header = Base64UrlEncode("""{"alg":"none","typ":"JWT"}""");
            var payload = Base64UrlEncode(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] = role,
                ["language"] = language
            }));
            return $"{header}.{payload}.fakesig";
        }

        private static string Base64UrlEncode(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static HttpResponseMessage JsonOk<T>(T body) =>
            new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

        private static HttpResponseMessage ApiError(string message, HttpStatusCode status = HttpStatusCode.BadRequest) =>
            new(status) { Content = new StringContent($"{{\"error\":\"{message}\"}}", Encoding.UTF8, "application/json") };

        private LoginViewModel BuildViewModel(
            Func<HttpRequestMessage, HttpResponseMessage> authHandler,
            Func<HttpRequestMessage, HttpResponseMessage>? userHandler = null,
            Mock<INavigationService>? navService = null,
            Mock<ILanguageService>? languageService = null,
            UserStore? store = null)
        {
            var authClient = new AuthApiClient(new HttpClient(new FakeHttpMessageHandler(authHandler)) { BaseAddress = new Uri("http://test") });
            var userClient = new UserApiClient(new HttpClient(new FakeHttpMessageHandler(userHandler ?? (_ => ApiError("not found", HttpStatusCode.NotFound)))) { BaseAddress = new Uri("http://test") });

            return new LoginViewModel(
                authClient,
                userClient,
                (navService ?? new Mock<INavigationService>()).Object,
                store ?? new UserStore(),
                (languageService ?? new Mock<ILanguageService>()).Object,
                new SessionService(),
                new AuthTokenStore());
        }

        // ── OnNavigatedToAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task OnNavigatedTo_ResetsLanguageToEnglish()
        {
            var vm = BuildViewModel(_ => ApiError("unused"));

            vm.SelectedLanguage = "he";
            await vm.OnNavigatedToAsync();

            vm.SelectedLanguage.Should().Be("en");
        }

        [Fact]
        public async Task OnNavigatedTo_AppliesEnglishToLanguageService()
        {
            var langService = new Mock<ILanguageService>();
            var vm = BuildViewModel(_ => ApiError("unused"), languageService: langService);

            await vm.OnNavigatedToAsync();

            langService.Verify(l => l.ApplyLanguage("en"), Times.AtLeastOnce);
        }

        // ── SendVerificationCode / Login ───────────────────────────────────────

        [Fact]
        public async Task SendVerificationCode_SetsErrorMessage_WhenAuthFails()
        {
            var vm = BuildViewModel(_ => ApiError("User not found", HttpStatusCode.NotFound));

            vm.NationalId = "000000000";
            await vm.SendVerificationCodeCommand.ExecuteAsync(null);

            vm.WasCodeSent.Should().BeFalse();
            vm.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SendVerificationCode_LeavesWasCodeSentFalse_OnAnyFailure()
        {
            var vm = BuildViewModel(_ => ApiError("Unauthorized", HttpStatusCode.Unauthorized));

            vm.NationalId = "999999999";
            await vm.SendVerificationCodeCommand.ExecuteAsync(null);

            vm.WasCodeSent.Should().BeFalse();
        }

        [Fact]
        public async Task Login_SetsErrorMessage_WhenVerificationCodeIsInvalid()
        {
            var vm = BuildViewModel(_ => ApiError("Invalid code", HttpStatusCode.Unauthorized));

            vm.NationalId = "123456789";
            vm.VerificationCode = "BADCODE";
            await vm.LoginCommand.ExecuteAsync(null);

            vm.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_SetsErrorMessage_WhenCodeIsExpired()
        {
            var vm = BuildViewModel(_ => ApiError("Code expired", HttpStatusCode.Unauthorized));

            vm.NationalId = "123456789";
            vm.VerificationCode = "OLDCODE";
            await vm.LoginCommand.ExecuteAsync(null);

            vm.ErrorMessage.Should().Contain("expired");
        }

        [Fact]
        public async Task Login_PopulatesUserStore_WhenCredentialsValid()
        {
            const int userId = 7;
            var jwt = MakeJwt(userId);

            var store = new UserStore();
            var vm = BuildViewModel(
                _ => JsonOk(new { token = jwt, expiresAt = DateTime.UtcNow.AddHours(1) }),
                _ => JsonOk(new
                {
                    id = userId, userName = "Test", email = "t@t.com", nationalId = "123456789",
                    profilePicturePath = (string?)null, language = "en", phoneNumber = (string?)null,
                    gender = 3, role = "Admin",
                    gradeId = (int?)null, classNum = (int?)null,
                    preferredMentorGender = (int?)null, preferredMenteeGender = (int?)null,
                    mentorSubjectId = (int?)null, maxMentees = (int?)null, menteeSubjectId = (int?)null
                }),
                store: store);

            vm.NationalId = "123456789";
            vm.VerificationCode = "VALID1";
            await vm.LoginCommand.ExecuteAsync(null);

            store.User.Should().NotBeNull();
            store.User!.Id.Should().Be(userId);
        }

        [Fact]
        public async Task Login_TriggersNavigation_WhenCredentialsValid()
        {
            const int userId = 8;
            var jwt = MakeJwt(userId);
            var navService = new Mock<INavigationService>();

            var vm = BuildViewModel(
                _ => JsonOk(new { token = jwt, expiresAt = DateTime.UtcNow.AddHours(1) }),
                _ => JsonOk(new
                {
                    id = userId, userName = "Test", email = "t@t.com", nationalId = "123456789",
                    profilePicturePath = (string?)null, language = "en", phoneNumber = (string?)null,
                    gender = 3, role = "Admin",
                    gradeId = (int?)null, classNum = (int?)null,
                    preferredMentorGender = (int?)null, preferredMenteeGender = (int?)null,
                    mentorSubjectId = (int?)null, maxMentees = (int?)null, menteeSubjectId = (int?)null
                }),
                navService: navService);

            vm.NationalId = "123456789";
            vm.VerificationCode = "VALID2";
            await vm.LoginCommand.ExecuteAsync(null);

            navService.Verify(
                n => n.NavigateToAsync<AuthenticatedDashboardViewModel>(),
                Times.Once);
        }
    }
}
