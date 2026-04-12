using FluentAssertions;
using MentoringApp.Model;
using MentoringApp.Model.User;
using Xunit;

namespace MentoringApp.Tests
{
    public class UserModelTests
    {
        private static StudentModel MakeUser() =>
            new StudentModel(1, "test@test.com", "Test", "123456789",
                new Grade { Id = 1, Name = "Grade 9", Num = 9 });

        [Fact]
        public void IsValidProfilePicture_ReturnsTrue_ForJpg()
        {
            var user = MakeUser();

            user.IsValidProfilePicture("photo.jpg").Should().BeTrue();
        }

        [Fact]
        public void IsValidProfilePicture_ReturnsTrue_ForJpeg()
        {
            var user = MakeUser();

            user.IsValidProfilePicture("photo.jpeg").Should().BeTrue();
        }

        [Fact]
        public void IsValidProfilePicture_ReturnsTrue_ForPng()
        {
            var user = MakeUser();

            user.IsValidProfilePicture("photo.png").Should().BeTrue();
        }

        [Fact]
        public void IsValidProfilePicture_ReturnsFalse_ForGif()
        {
            var user = MakeUser();

            user.IsValidProfilePicture("photo.gif").Should().BeFalse();
        }

        [Fact]
        public void IsValidProfilePicture_ReturnsFalse_ForBmp()
        {
            var user = MakeUser();

            user.IsValidProfilePicture("photo.bmp").Should().BeFalse();
        }

        [Fact]
        public void IsValidProfilePicture_IsCaseInsensitive_ForJPG()
        {
            var user = MakeUser();

            user.IsValidProfilePicture("photo.JPG").Should().BeTrue();
        }

        [Fact]
        public void IsValidProfilePicture_ReturnsFalse_ForNoExtension()
        {
            var user = MakeUser();

            user.IsValidProfilePicture("photowithoutext").Should().BeFalse();
        }
    }
}
