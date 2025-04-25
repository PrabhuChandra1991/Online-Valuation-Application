using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;
using System.IO;

namespace SKCE.Examination.Services.Helpers
{
    public static class ExcelExportHelper
    {
        public static MemoryStream GenerateExcel(List<dynamic> groupedData, string programType)
        {
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
                programType == "PG" ? "Export_Format_PG.xlsx" : "Export_Format_UG.xlsx");

            using var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            using (var document = SpreadsheetDocument.Open(memoryStream, true))
            {
                var workbookPart = document.WorkbookPart;
                var worksheetPart = workbookPart.WorksheetParts.First();
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                int startRow = 2; // Assuming header is in Row 1

                foreach (var item in groupedData)
                {
                    var row = new Row();
                    row.Append(CreateCell(item.DummyNumber.ToString()));

                    int totalQuestions = 20;
                    for (int i = 1; i <= totalQuestions; i++)
                    {
                        if (item.QuestionMarks is Dictionary<int, object> questionMarks)
                        {
                            questionMarks.TryGetValue(i, out object mark);
                            row.Append(CreateCell(mark?.ToString() ?? ""));
                        }
                    }

                    row.Append(CreateCell(item.PartATotal.ToString()));
                    row.Append(CreateCell(item.PartBTotal.ToString()));
                    if (programType == "PG")
                        row.Append(CreateCell(item.PartCTotal.ToString()));

                    row.Append(CreateCell(item.GrandTotal.ToString()));
                    sheetData.Append(row);
                }

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
        private static Cell CreateCell(string value)
        {
            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(value)
            };
        }
    }
}
