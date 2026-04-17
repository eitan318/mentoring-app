using ClosedXML.Excel;
using MentoringApp.Model;
using MentoringApp.Model.User;
using System;
using System.IO;
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

        // ──── Import ────────────────────────────────────────────────────────

        /// <summary>
        /// Imports users from an Excel file.
        /// Expected columns: NationalId | Email | UserName
        /// </summary>
        public async Task<Result<int>> ImportStudentsFromExcelAsync(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1); // skip header row

                int successCount = 0;

                foreach (var row in rows)
                {
                    string nationalId = row.Cell(1).GetString()?.Trim() ?? "";
                    string email      = row.Cell(2).GetString()?.Trim() ?? "";
                    string userName   = row.Cell(3).GetString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(nationalId) ||
                        string.IsNullOrWhiteSpace(email)      ||
                        string.IsNullOrWhiteSpace(userName))
                        continue;

                    var student = new StudentModel(
                        id:         0,
                        email:      email,
                        userName:   userName,
                        nationalId: nationalId,
                        grade:      new Grade { Id = 1, Name = "Imported", Num = 0 }
                    );

                    var result = await _userService.CreateUserAsync(student);
                    if (result.Success) successCount++;
                }

                return Result<int>.Ok(successCount);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to import students: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports supervisors from an Excel file.
        /// Expected columns: NationalId | Email | UserName
        /// </summary>
        public async Task<Result<int>> ImportSupervisorsFromExcelAsync(string filePath)
        {
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1); // skip header row

                int successCount = 0;

                foreach (var row in rows)
                {
                    string nationalId = row.Cell(1).GetString()?.Trim() ?? "";
                    string email      = row.Cell(2).GetString()?.Trim() ?? "";
                    string userName   = row.Cell(3).GetString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(nationalId) ||
                        string.IsNullOrWhiteSpace(email)      ||
                        string.IsNullOrWhiteSpace(userName))
                        continue;

                    var supervisor = new SupervisorModel(
                        id:         0,
                        email:      email,
                        userName:   userName,
                        nationalId: nationalId
                    );

                    var result = await _userService.CreateUserAsync(supervisor);
                    if (result.Success) successCount++;
                }

                return Result<int>.Ok(successCount);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to import supervisors: {ex.Message}");
            }
        }

        // ──── Template Generation ────────────────────────────────────────────

        /// <summary>
        /// Generates an example Excel template and saves it to <paramref name="savePath"/>.
        /// </summary>
        public Result GenerateTemplate(bool isSupervisor, string savePath)
        {
            try
            {
                using var workbook  = new XLWorkbook();
                string sheetName    = isSupervisor ? "Supervisors" : "Students";
                var worksheet       = workbook.Worksheets.Add(sheetName);

                // Header row
                worksheet.Cell(1, 1).Value = "NationalId";
                worksheet.Cell(1, 2).Value = "Email";
                worksheet.Cell(1, 3).Value = "UserName";

                // Style the header
                var headerRow = worksheet.Range("A1:C1");
                headerRow.Style.Font.Bold        = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                headerRow.Style.Font.FontColor   = XLColor.White;

                // Example data row
                if (isSupervisor)
                {
                    worksheet.Cell(2, 1).Value = "S-123456";
                    worksheet.Cell(2, 2).Value = "supervisor@example.com";
                    worksheet.Cell(2, 3).Value = "Dr. Smith";
                }
                else
                {
                    worksheet.Cell(2, 1).Value = "N-123456";
                    worksheet.Cell(2, 2).Value = "student@example.com";
                    worksheet.Cell(2, 3).Value = "John Doe";
                }

                // Column widths
                worksheet.Column(1).Width = 18;
                worksheet.Column(2).Width = 30;
                worksheet.Column(3).Width = 22;

                workbook.SaveAs(savePath);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to generate template: {ex.Message}");
            }
        }
    }
}
