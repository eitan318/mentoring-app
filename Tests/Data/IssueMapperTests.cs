using FluentAssertions;
using MentoringApp.Data.DTO;
using MentoringApp.Model;
using MentoringApp.Service.Mapping;
using Xunit;

namespace MentoringApp.Tests.Data
{
    public class IssueMapperTests
    {
        // ── helpers ────────────────────────────────────────────────────────────

        private static IssueDto MakeDto(
            int id = 1,
            string description = "Test issue",
            int categoryId = 10,
            int reportedByUserId = 7,
            int isResolved = 0,
            string creationDate = "2024-01-15T10:30:00") =>
            new IssueDto
            {
                Id = id,
                Description = description,
                CategoryId = categoryId,
                ReportedByUserId = reportedByUserId,
                IsResolved = isResolved,
                CreationDate = creationDate
            };

        private static IssueCategoryModel MakeCategory(int id = 10, string name = "Behaviour") =>
            new IssueCategoryModel(name, id);

        // ── ToModel ────────────────────────────────────────────────────────────

        [Fact]
        public void ToModel_MapsDescriptionCorrectly()
        {
            var dto = MakeDto(description: "Bullying incident");
            var category = MakeCategory();

            var model = IssueMapper.ToModel(dto, category);

            model.Description.Should().Be("Bullying incident");
        }

        [Fact]
        public void ToModel_MapsIdCorrectly()
        {
            var dto = MakeDto(id: 99);
            var category = MakeCategory();

            var model = IssueMapper.ToModel(dto, category);

            model.Id.Should().Be(99);
        }

        [Fact]
        public void ToModel_MapsIsResolved_WhenIsResolvedIsNonZero()
        {
            var dto = MakeDto(isResolved: 1);
            var category = MakeCategory();

            var model = IssueMapper.ToModel(dto, category);

            model.IsResolved.Should().BeTrue();
        }

        [Fact]
        public void ToModel_MapsIsNotResolved_WhenIsResolvedIsZero()
        {
            var dto = MakeDto(isResolved: 0);
            var category = MakeCategory();

            var model = IssueMapper.ToModel(dto, category);

            model.IsResolved.Should().BeFalse();
        }

        [Fact]
        public void ToModel_MapsCreationDate_FromIsoString()
        {
            var dto = MakeDto(creationDate: "2024-06-20T14:00:00");
            var category = MakeCategory();

            var model = IssueMapper.ToModel(dto, category);

            model.CreationDate.Should().Be(new DateTime(2024, 6, 20, 14, 0, 0));
        }

        [Fact]
        public void ToModel_MapsReportedByUserId()
        {
            var dto = MakeDto(reportedByUserId: 42);
            var category = MakeCategory();

            var model = IssueMapper.ToModel(dto, category);

            model.ReportedByUserId.Should().Be(42);
        }

        [Fact]
        public void ToModel_MapsCategory()
        {
            var dto = MakeDto(categoryId: 5);
            var category = new IssueCategoryModel("Attendance", id: 5);

            var model = IssueMapper.ToModel(dto, category);

            model.Category.Should().BeSameAs(category);
            model.Category.Id.Should().Be(5);
            model.Category.Name.Should().Be("Attendance");
        }

        // ── ToModels ───────────────────────────────────────────────────────────

        [Fact]
        public void ToModels_MapsMultipleDtos()
        {
            var dtos = new[]
            {
                MakeDto(id: 1, categoryId: 10),
                MakeDto(id: 2, categoryId: 20)
            };
            var categories = new[]
            {
                MakeCategory(id: 10, name: "Cat A"),
                MakeCategory(id: 20, name: "Cat B")
            };

            var models = IssueMapper.ToModels(dtos, categories).ToList();

            models.Should().HaveCount(2);
            models[0].Id.Should().Be(1);
            models[1].Id.Should().Be(2);
        }

        [Fact]
        public void ToModels_UsesUnknownCategory_WhenCategoryNotFound()
        {
            var dtos = new[] { MakeDto(categoryId: 999) };
            var categories = Array.Empty<IssueCategoryModel>();

            var models = IssueMapper.ToModels(dtos, categories).ToList();

            models.Should().HaveCount(1);
            models[0].Category.Id.Should().Be(999);
            models[0].Category.Name.Should().Be("Unknown");
        }

        [Fact]
        public void ToModels_MatchesCategoryById()
        {
            var dtos = new[] { MakeDto(categoryId: 7) };
            var categories = new[]
            {
                MakeCategory(id: 3, name: "Wrong"),
                MakeCategory(id: 7, name: "Correct")
            };

            var models = IssueMapper.ToModels(dtos, categories).ToList();

            models[0].Category.Name.Should().Be("Correct");
        }

        [Fact]
        public void ToModels_ReturnsEmptyEnumerable_ForEmptyInput()
        {
            var dtos = Array.Empty<IssueDto>();
            var categories = new[] { MakeCategory() };

            var models = IssueMapper.ToModels(dtos, categories);

            models.Should().BeEmpty();
        }
    }
}
