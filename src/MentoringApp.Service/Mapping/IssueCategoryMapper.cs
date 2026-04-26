using MentoringApp.Data.Dao;
using MentoringApp.Data.DTO;
using MentoringApp.Model;

namespace MentoringApp.Service.Mapping
{

    public static class IssueCategoryMapper
    {
        public static IssueCategoryModel ToModel(IssueCategoryDao dto)
        {
            return new IssueCategoryModel(dto.Name, dto.Id);
        }

        public static IEnumerable<IssueCategoryModel> ToModels(IEnumerable<IssueCategoryDao> dtos)
        {
            return dtos.Select(ToModel);
        }
    }
    
}
