using FluentAssertions;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;
using Moq;
using Xunit;

namespace MentoringApp.Tests.Service
{
    public class ReviewServiceTests
    {
        // ── CreateReviewAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task CreateReview_Fails_WhenContentIsEmpty()
        {
            var sut = new ReviewService(new Mock<IReviewRepo>().Object);

            var result = await sut.CreateReviewAsync("", pairId: 1, authorUserId: 1, amountOfHours: 1.5);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("empty");
        }

        [Fact]
        public async Task CreateReview_Fails_WhenContentIsWhitespace()
        {
            var sut = new ReviewService(new Mock<IReviewRepo>().Object);

            var result = await sut.CreateReviewAsync("   ", pairId: 1, authorUserId: 1, amountOfHours: 1.5);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreateReview_Fails_WhenHoursIsZero()
        {
            var sut = new ReviewService(new Mock<IReviewRepo>().Object);

            var result = await sut.CreateReviewAsync("Good session", pairId: 1, authorUserId: 1, amountOfHours: 0);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("hours");
        }

        [Fact]
        public async Task CreateReview_Fails_WhenHoursIsNegative()
        {
            var sut = new ReviewService(new Mock<IReviewRepo>().Object);

            var result = await sut.CreateReviewAsync("Good session", pairId: 1, authorUserId: 1, amountOfHours: -1);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreateReview_Fails_WhenRepoReturnsFalse()
        {
            var reviewRepo = new Mock<IReviewRepo>();
            reviewRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()))
                      .ReturnsAsync(false);

            var sut = new ReviewService(reviewRepo.Object);

            var result = await sut.CreateReviewAsync("Good session", pairId: 1, authorUserId: 1, amountOfHours: 1.5);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to save");
        }

        [Fact]
        public async Task CreateReview_Succeeds_WhenContentAndHoursValid()
        {
            var reviewRepo = new Mock<IReviewRepo>();
            reviewRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), 1, 2, 1.5))
                      .ReturnsAsync(true);

            var sut = new ReviewService(reviewRepo.Object);

            var result = await sut.CreateReviewAsync("Great meeting", pairId: 1, authorUserId: 2, amountOfHours: 1.5);

            result.Success.Should().BeTrue();
        }

        // ── GetReviewsByPairAsync ──────────────────────────────────────────────

        [Fact]
        public async Task GetReviewsByPair_ReturnsMappedReviews()
        {
            var dtos = new[]
            {
                new ReviewDao { Id = 1, Content = "Session 1", Date = "2024-03-01T10:00:00", PairId = 5, AuthorUserId = 2, AmountOfHours = 1.0 },
                new ReviewDao { Id = 2, Content = "Session 2", Date = "2024-03-08T10:00:00", PairId = 5, AuthorUserId = 2, AmountOfHours = 2.0 }
            };

            var reviewRepo = new Mock<IReviewRepo>();
            reviewRepo.Setup(r => r.GetByPairAsync(5)).ReturnsAsync(dtos);

            var sut = new ReviewService(reviewRepo.Object);

            var result = await sut.GetReviewsByPairAsync(5);

            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data!.Select(r => r.Content).Should().BeEquivalentTo(["Session 1", "Session 2"]);
        }

        [Fact]
        public async Task GetReviewsByPair_ReturnsEmpty_WhenNoneExist()
        {
            var reviewRepo = new Mock<IReviewRepo>();
            reviewRepo.Setup(r => r.GetByPairAsync(99)).ReturnsAsync(Array.Empty<ReviewDao>());

            var sut = new ReviewService(reviewRepo.Object);

            var result = await sut.GetReviewsByPairAsync(99);

            result.Success.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        // ── GetReviewsByAuthorAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetReviewsByAuthor_ReturnsMappedReviews()
        {
            var dtos = new[]
            {
                new ReviewDao { Id = 3, Content = "My review", Date = "2024-04-01T09:00:00", PairId = 1, AuthorUserId = 7, AmountOfHours = 0.5 }
            };

            var reviewRepo = new Mock<IReviewRepo>();
            reviewRepo.Setup(r => r.GetByAuthorAsync(7)).ReturnsAsync(dtos);

            var sut = new ReviewService(reviewRepo.Object);

            var result = await sut.GetReviewsByAuthorAsync(7);

            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle(r => r.Content == "My review" && r.AmountOfHours == 0.5);
        }
    }
}
