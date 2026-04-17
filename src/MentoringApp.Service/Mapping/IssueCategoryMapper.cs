using MentoringApp.Data.DTO;
using MentoringApp.Model;

namespace MentoringApp.Service.Mapping
{

    public static class IssueCategoryMapper
    {
        public static IssueCategoryModel ToModel(IssueCategoryDto dto)
        {
            return new IssueCategoryModel(dto.Name, dto.Id);
        }

        public static IEnumerable<IssueCategoryModel> ToModels(IEnumerable<IssueCategoryDto> dtos)
        {
            return dtos.Select(ToModel);
        }
    }
    
}
