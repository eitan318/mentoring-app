using ClosedXML.Excel;
using System;
using System.IO;

var workbook = new XLWorkbook();
var ws = workbook.Worksheets.Add("Users");

ws.Cell(1, 1).Value = "National ID";
ws.Cell(1, 2).Value = "Email";
ws.Cell(1, 3).Value = "User Name";

ws.Cell(2, 1).Value = "QAZ123456";
ws.Cell(2, 2).Value = "johndoe@example.com";
ws.Cell(2, 3).Value = "John Doe";

ws.Cell(3, 1).Value = "WSX987654";
ws.Cell(3, 2).Value = "janedoe@example.com";
ws.Cell(3, 3).Value = "Jane Doe";

string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
string filePath = Path.Combine(desktopPath, "example_users.xlsx");
workbook.SaveAs(filePath);
Console.WriteLine($"Successfully saved to {filePath}");
