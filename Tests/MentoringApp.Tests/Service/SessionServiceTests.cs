using FluentAssertions;
using MentoringApp.Service;
using System.Text.Json;
using Xunit;

namespace MentoringApp.Tests.Service
{
    public class SessionServiceTests : IDisposable
    {
        // Mirror the path used inside SessionService so tests can write directly to it.
        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MentoringApp", "session.json");

        private readonly SessionService _sut = new();

        public void Dispose()
        {
            // Always clean up so tests don't bleed state into one another.
            _sut.ClearSession();
        }

        [Fact]
        public void SaveSession_ThenLoadSession_ReturnsSameUserId()
        {
            _sut.SaveSession(42);

            var loaded = _sut.LoadSession();

            loaded.Should().Be(42);
        }

        [Fact]
        public void LoadSession_ReturnsNull_WhenFileDoesNotExist()
        {
            _sut.ClearSession(); // make sure no file is present

            var loaded = _sut.LoadSession();

            loaded.Should().BeNull();
        }

        [Fact]
        public void ClearSession_DeletesFile_SoLoadReturnsNull()
        {
            _sut.SaveSession(99);
            _sut.ClearSession();

            var loaded = _sut.LoadSession();

            loaded.Should().BeNull();
            File.Exists(SessionFilePath).Should().BeFalse();
        }

        [Fact]
        public void LoadSession_ReturnsNull_ForInvalidJson()
        {
            // Write garbage directly to the file so the deserialiser throws.
            Directory.CreateDirectory(Path.GetDirectoryName(SessionFilePath)!);
            File.WriteAllText(SessionFilePath, "this-is-not-valid-json{{{{");

            var loaded = _sut.LoadSession();

            loaded.Should().BeNull();
        }

        [Fact]
        public void LoadSession_ReturnsNull_WhenUserIdIsZero()
        {
            // SessionService returns null for UserId == 0.
            Directory.CreateDirectory(Path.GetDirectoryName(SessionFilePath)!);
            File.WriteAllText(SessionFilePath, JsonSerializer.Serialize(new { UserId = 0 }));

            var loaded = _sut.LoadSession();

            loaded.Should().BeNull();
        }

        [Fact]
        public void SaveAndLoad_MultipleIds_LastOneWins()
        {
            _sut.SaveSession(5);
            _sut.SaveSession(10);

            var loaded = _sut.LoadSession();

            loaded.Should().Be(10);
        }
    }
}
