using MentoringApp.Data.Dao;
using MentoringApp.Data.DTO;
using MentoringApp.Model;

namespace MentoringApp.Service.Mapping
{

    /// <summary>Static mapper converting <see cref="IssueCategoryDao"/> rows into <see cref="IssueCategoryModel"/> objects.</summary>
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
