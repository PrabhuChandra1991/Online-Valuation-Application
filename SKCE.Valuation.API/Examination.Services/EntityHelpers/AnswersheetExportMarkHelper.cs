using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.EntityHelpers
{
    public class AnswersheetExportMarkHelper
    { 
        private readonly ExaminationDbContext _context;

        public AnswersheetExportMarkHelper(ExaminationDbContext context)
        {
            _context = context;
        }


        public async Task<(MemoryStream, string)> ExportMarksAsync(
            long institutionId, long courseId, string examYear, string examMonth, string examType)
        {

            var examinations = await this._context.Examinations
                .Where(x => x.InstitutionId == institutionId && x.CourseId == courseId
                && x.ExamYear == examYear && x.ExamMonth == examMonth && x.ExamType == examType).ToListAsync();

            var examinationIds = examinations.Select(x => x.ExaminationId).ToList();

            var answersheets = await this._context.Answersheets
                .Where(x =>
                examinationIds.Contains(x.ExaminationId)
                && x.IsActive
                ).ToListAsync();

            if (answersheets.Any(x => x.IsEvaluateCompleted == false))
            {
                throw new Exception("EVALUATION-NOT-COMPLETED");
            }


            var answersheetIds = answersheets.Select(x => x.AnswersheetId).ToList();

            var answersheetQuestionwiseMarks = await this._context.AnswersheetQuestionwiseMarks
                .Where(x => answersheetIds.Contains(x.AnswersheetId) && x.IsActive).ToListAsync();

            var degreeType = _context.DegreeTypes
                .FirstOrDefault(x => x.DegreeTypeId == examinations.First().DegreeTypeId)?.Name;

            var institutionCode = _context.Institutions
                .Where(x => x.InstitutionId == institutionId)
                .Select(x => x.Code)
                .FirstOrDefault();

            var courseCode = _context.Courses
                .Where(x => x.CourseId == courseId)
                .Select(x => x.Code)
                .FirstOrDefault();

            string templatePath =
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Export Templates",
                degreeType == "PG" ? "Export_Format_PG.xlsx" : "Export_Format_UG.xlsx");

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

                int partAQuestions = 10;
                int partBQuestions = (degreeType == "UG") ? 20 : 18;
                int partCQuestions = (degreeType == "UG") ? 0 : 19;
                int sno = 1;

                foreach (var answersheet in answersheets)
                {
                    var asQnItems = answersheetQuestionwiseMarks.Where(x => x.AnswersheetId == answersheet.AnswersheetId).ToList();

                    decimal partATotal = 0;
                    decimal partBTotal = 0;
                    decimal partCTotal = 0;

                    var row = new Row();
                    row.Append(CreateCell(sno));
                    row.Append(CreateStringCell(institutionCode));
                    row.Append(CreateStringCell(courseCode));
                    row.Append(CreateStringCell(answersheet.DummyNumber));

                    // PART A
                    for (int i = 1; i <= partAQuestions; i++)
                    {
                        var asQnItem = asQnItems.Where(x => x.QuestionNumber == i).FirstOrDefault();
                        decimal mark = asQnItem != null ? asQnItem.ObtainedMark : 0;
                        row.Append(CreateCell(mark));
                    }

                    partATotal = asQnItems.Where(x => x.QuestionPartName == "A").Sum(x => x.ObtainedMark);
                    row.Append(CreateCell(partATotal));

                    // PART B
                    for (int i = 11; i <= partBQuestions; i++)
                    {
                        var asQnItem1 = asQnItems.Where(x => x.QuestionNumber == i && x.QuestionNumberSubNum == 1).FirstOrDefault();
                        decimal mark1 = asQnItem1 != null ? asQnItem1.ObtainedMark : 0;
                        row.Append(CreateCell(mark1));

                        var asQnItem2 = asQnItems.Where(x => x.QuestionNumber == i && x.QuestionNumberSubNum == 2).FirstOrDefault();
                        decimal mark2 = asQnItem2 != null ? asQnItem2.ObtainedMark : 0;
                        row.Append(CreateCell(mark2));

                        decimal totalMark = mark1 + mark2;

                        row.Append(CreateCell(totalMark));
                    }


                    var partBItems =
                        asQnItems.Where(x => x.QuestionPartName == "B")
                        .GroupBy(x => x.QuestionGroupName).ToList();

                    var partBGroups = partBItems.Select(x => new
                    {
                        gepName = x.Key,
                        totalMarks = x.GroupBy(y => y.QuestionNumber).Select(y => new
                        {
                            QnNo = y.Key,
                            totalMarks = y.Sum(x => x.ObtainedMark)
                        }).Max(x => x.totalMarks)
                    });

                    partBTotal = partBGroups.Sum(x => x.totalMarks);
                    row.Append(CreateCell(partBTotal));

                    // PART C
                    if (degreeType == "PG")
                    {
                        for (int i = 19; i <= partCQuestions; i++)
                        {
                            var asQnItem1 = asQnItems.Where(x => x.QuestionNumber == i && x.QuestionNumberSubNum == 1).FirstOrDefault();
                            decimal mark1 = asQnItem1 != null ? asQnItem1.ObtainedMark : 0;
                            row.Append(CreateCell(mark1));

                            var asQnItem2 = asQnItems.Where(x => x.QuestionNumber == i && x.QuestionNumberSubNum == 2).FirstOrDefault();
                            decimal mark2 = asQnItem2 != null ? asQnItem2.ObtainedMark : 0;
                            row.Append(CreateCell(mark2));

                            decimal totalMark = mark1 + mark2;

                            row.Append(CreateCell(totalMark));
                        }

                        var partCGroups = asQnItems
                            .Where(x => x.QuestionPartName == "C")
                            .GroupBy(x => x.QuestionGroupName)
                            .Select(x => new
                            {
                                grpName = x.Key,
                                totalMarks = x.GroupBy(y => y.QuestionNumber).Select(y => new
                                {
                                    QnNo = y.Key,
                                    totalMarks = y.Sum(x => x.ObtainedMark)
                                }).Max(x => x.totalMarks)
                            });

                        partCTotal = partCGroups.Sum(x => x.totalMarks);
                        row.Append(CreateCell(partCTotal));
                    }

                    //Grand Total
                    decimal grandTotalMark = partATotal + partBTotal + partCTotal;
                    row.Append(CreateCell(grandTotalMark));

                    sheetData.Append(row);
                    sno++;
                }

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            memoryStream.Position = 0;

            return await Task.FromResult((memoryStream, $"MarksReport_{institutionCode}_{examYear}_{examMonth}_{courseCode}_{degreeType}.xlsx"));

        }

        private static Cell CreateStringCell(string value)
        {
            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(value)
            };
        }

        private static Cell CreateCell(decimal value)
        {
            return new Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(value)
            };
        }

    }
}
