using ClosedXML.Excel;
using MentoringApp.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MentoringApp.Service
{
    public class ExcelImportService
    {
        private readonly UserService _userService;

        public ExcelImportService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<Result<int>> ImportUsersFromExcelAsync(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1); // skip header
                
                int successCount = 0;
                
                foreach (var row in rows)
                {
                    string nationalId = row.Cell(1).GetString()?.Trim() ?? "";
                    string email = row.Cell(2).GetString()?.Trim() ?? "";
                    string userName = row.Cell(3).GetString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(nationalId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName))
                        continue;

                    var student = new Student(
                        id: 0, 
                        email: email, 
                        userName: userName, 
                        nationalId: nationalId, 
                        grade: new Grade { Id = 1, Name = "Imported", Num = 0 }
                    );

                    var result = await _userService.CreateUserAsync(student);
                    if (result.Success)
                    {
                        successCount++;
                    }
                }

                return Result<int>.Ok(successCount);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to import from Excel: {ex.Message}");
            }
        }
    }
}
