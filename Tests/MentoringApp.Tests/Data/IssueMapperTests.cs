using FluentAssertions;
using MentoringApp.Data.DTO;
using MentoringApp.Model;
using MentoringApp.Service.Mapping;
using Xunit;

namespace MentoringApp.Tests.Data
{
    public class IssueMapperTests
    {
        private static IssueDto MakeDto(int id=1,string description="Test issue",int categoryId=10,int reportedByUserId=7,int isResolved=0,string creationDate="2024-01-15T10:30:00") =>
            new IssueDto{Id=id,Description=description,CategoryId=categoryId,ReportedByUserId=reportedByUserId,IsResolved=isResolved,CreationDate=creationDate};
        private static IssueCategoryModel MakeCategory(int id=10,string name="Behaviour") => new IssueCategoryModel(name,id);

        [Fact] public void ToModel_MapsDescriptionCorrectly(){IssueMapper.ToModel(MakeDto(description:"Bullying incident"),MakeCategory()).Description.Should().Be("Bullying incident");}
        [Fact] public void ToModel_MapsIdCorrectly(){IssueMapper.ToModel(MakeDto(id:99),MakeCategory()).Id.Should().Be(99);}
        [Fact] public void ToModel_MapsIsResolved_WhenNonZero(){IssueMapper.ToModel(MakeDto(isResolved:1),MakeCategory()).IsResolved.Should().BeTrue();}
        [Fact] public void ToModel_MapsIsNotResolved_WhenZero(){IssueMapper.ToModel(MakeDto(isResolved:0),MakeCategory()).IsResolved.Should().BeFalse();}
        [Fact] public void ToModel_MapsCreationDate(){IssueMapper.ToModel(MakeDto(creationDate:"2024-06-20T14:00:00"),MakeCategory()).CreationDate.Should().Be(new DateTime(2024,6,20,14,0,0));}
        [Fact] public void ToModel_MapsReportedByUserId(){IssueMapper.ToModel(MakeDto(reportedByUserId:42),MakeCategory()).ReportedByUserId.Should().Be(42);}
        [Fact] public void ToModel_MapsCategory(){var cat=new IssueCategoryModel("Attendance",id:5);IssueMapper.ToModel(MakeDto(categoryId:5),cat).Category.Should().BeSameAs(cat);}
        [Fact] public void ToModels_MapsMultipleDtos(){IssueMapper.ToModels(new[]{MakeDto(id:1,categoryId:10),MakeDto(id:2,categoryId:20)},new[]{MakeCategory(id:10,name:"A"),MakeCategory(id:20,name:"B")}).Should().HaveCount(2);}
        [Fact] public void ToModels_UsesUnknownCategory_WhenNotFound(){IssueMapper.ToModels(new[]{MakeDto(categoryId:999)},Array.Empty<IssueCategoryModel>()).First().Category.Name.Should().Be("Unknown");}
        [Fact] public void ToModels_ReturnsEmpty_ForEmptyInput(){IssueMapper.ToModels(Array.Empty<IssueDto>(),new[]{MakeCategory()}).Should().BeEmpty();}
    }
}
