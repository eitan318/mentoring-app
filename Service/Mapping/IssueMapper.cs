using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Service.Mapping
{ 
    public static class IssueMapper
    {
        public static IssueModel ToModel(IssueDto dto, IssueCategoryModel category)
        {
            return new IssueModel(dto.Description, category, dto.IsResolved != 0)
            {
                Id = dto.Id,
                CreationDate = DateTime.Parse(dto.CreationDate)
            };
        }

        public static IEnumerable<IssueModel> ToModels(IEnumerable<IssueDto> dtos, IEnumerable<IssueCategoryModel> categories)
        {
            return dtos.Select(dto =>
            {
                var category = categories.FirstOrDefault(c => c.Id == dto.CategoryId)
                                ?? new IssueCategoryModel { Id = dto.CategoryId, Name = "Unknown" };
                return ToModel(dto, category);
            });
        }
    }
   
}

