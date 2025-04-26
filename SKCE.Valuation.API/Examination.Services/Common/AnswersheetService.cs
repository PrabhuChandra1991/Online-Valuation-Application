using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.EntityHelpers;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class AnswersheetService
    {
        private readonly ExaminationDbContext _context;
        private readonly IUserService _userService;
        private readonly EmailService _emailService;

        public AnswersheetService(ExaminationDbContext context, IUserService userService, EmailService emailService)
        {
            _context = context;
            _userService = userService;
            _emailService = emailService;
        }

        public async Task<IEnumerable<Answersheet>> GetAllAnswersheetsAsync()
        {
            return await _context.Answersheets.ToListAsync();
        }

        public async Task<Answersheet?> GetAnswersheetAsync(long id)
        {
            return await _context.Answersheets.FindAsync(id);
        }

        public async Task<IEnumerable<AnswerManagementDto>> GetAnswersheetDetailsAsync(
            long? institutionId = null, long? courseId = null, long? allocatedToUserId = null, long? answersheetId = null)
        {
            var resultItems =
                await (from answersheet in _context.Answersheets
                       join examination in this._context.Examinations on answersheet.ExaminationId equals examination.ExaminationId
                       join institute in _context.Institutions on examination.InstitutionId equals institute.InstitutionId
                       join course in _context.Courses on examination.CourseId equals course.CourseId
                       join dtype in _context.DegreeTypes on examination.DegreeTypeId equals dtype.DegreeTypeId

                       join allocatedUser in this._context.Users on answersheet.AllocatedToUserId equals allocatedUser.UserId into allocatedUserResults
                       from allocatedUserResult in allocatedUserResults.DefaultIfEmpty()

                       where
                       answersheet.IsActive == true
                       && (examination.InstitutionId == institutionId || institutionId == null)
                       && (examination.CourseId == courseId || courseId == null)
                       && (answersheet.AllocatedToUserId == allocatedToUserId || allocatedToUserId == null)
                       && (answersheet.AnswersheetId == answersheetId || answersheetId == null)

                       select new AnswerManagementDto
                       {
                           AnswersheetId = answersheet.AnswersheetId,
                           InstitutionId = examination.InstitutionId,
                           InstitutionName = institute.Name,
                           RegulationYear = examination.RegulationYear,
                           BatchYear = examination.BatchYear,
                           DegreeTypeName = dtype.Name,
                           ExamType = examination.ExamType,
                           Semester = (int)examination.Semester,
                           CourseId = examination.CourseId,
                           CourseCode = course.Code,
                           CourseName = course.Name,
                           ExamMonth = examination.ExamMonth,
                           ExamYear = examination.ExamYear,
                           DummyNumber = answersheet.DummyNumber,
                           UploadedBlobStorageUrl = answersheet.UploadedBlobStorageUrl,
                           AllocatedUserName = (allocatedUserResult != null ? allocatedUserResult.Name : string.Empty),
                           TotalObtainedMark = answersheet.TotalObtainedMark,
                           IsEvaluateCompleted = answersheet.IsEvaluateCompleted
                       }).ToListAsync();

            return resultItems;
        }

        public async Task<string> ImportDummyNumberByExcel(Stream excelStream, long loggedInUserId)
        {
            var helper = new AnswersheetHelper(this._context);
            var result = await helper.ImportDummyNumberByExcel(excelStream, loggedInUserId);
            return result.ToString();
        }


        public async Task<List<AnswersheetQuestionAnswerDto>> GetQuestionAndAnswersByAnswersheetIdAsync(long answersheetId)
        {
            var helper = new AnswersheetQuestionAnswerHelper(this._context);
            var result = await helper.GetQuestionAndAnswersByAnswersheetId(answersheetId);
            return result.ToList();
        }

        public async Task<List<AnswersheetConsolidatedDto>> GetExamConsolidatedAnswersheetsAsync(long institutionId)
        {
            var helper = new AnswersheetConsolidatedHelper(this._context);
            return await helper.GetConsolidatedItems(institutionId);
        }

        public async Task<List<AnswersheetQuestionwiseMark>> GetAnswersheetMarkAsync(long answersheetId)
        {
            var helper = new AnswersheetMarkTransHelper(this._context);
            return await helper.GetAnswersheetMarkAsync(answersheetId);
        }

        public async Task<decimal> SaveAnswersheetMarkAsync(AnswersheetQuestionwiseMark entity)
        {
            var helper = new AnswersheetMarkTransHelper(this._context);
            return await helper.SaveAnswersheetMarkAsync(entity);
        }

        public async Task<Boolean> CompleteEvaluationSync(long answersheetId, long evaluatedByUserId)
        {
            var helper = new AnswersheetMarkTransHelper(this._context);
            return await helper.CompleteEvaluationSync(answersheetId, evaluatedByUserId);
        }

        public async Task<Boolean> AllocateAnswerSheetsToUser(AnswersheetAllocateInputModel inputData)
        {
            long loggedInUserId = 1;
            var helper = new AnswersheetAllocateHelper(this._context);
            var response = await helper.AllocateAnswersheetsToUserRandomly(inputData, loggedInUserId);
            if (response)
            {
                var user = await _userService.GetUserOnlyByIdAsync(inputData.UserId);
                if (user != null)
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "SKCE Online Examination Platform: Answeersheets allocated", $"Hi {user.Name
                        },\n\nYou have been allocated with {inputData.Noofsheets
                        } answersheets for Evaluation.\n\n Please Evaluate. \n\n");
                }
            }
            return response;
        }
        public async Task<MemoryStream> ExportMarksAsync(long institutionId, string examYear, string examMonth, string degreeType)
        {
            var degreeTypeId = _context.DegreeTypes
                .Where(x => x.Name == degreeType)
                .Select(x => x.DegreeTypeId)
                .FirstOrDefault();
            var groupedData = (from e in _context.Examinations
                               join a in _context.Answersheets on e.ExaminationId equals a.ExaminationId
                               join qm in _context.AnswersheetQuestionwiseMarks on a.AnswersheetId equals qm.AnswersheetId
                               join dg in _context.DegreeTypes on e.DegreeTypeId equals dg.DegreeTypeId
                               where a.IsActive == true &&
                                     a.IsEvaluateCompleted == true &&
                                     e.InstitutionId == institutionId &&
                                     e.ExamYear == examYear &&
                                     e.ExamMonth == examMonth &&
                                     e.DegreeTypeId == degreeTypeId
                               group qm by new { a.DummyNumber } into g
                               select new
                               {
                                   DummyNumber = g.Key.DummyNumber,

                                   // Part A: Questions 1–10
                                   PartA_Total = g.Where(x => x.QuestionPartName == "A" && x.QuestionNumber >= 1 && x.QuestionNumber <= 10)
                                                  .Sum(x => x.ObtainedMark),

                                   // Part B
                                   PartB_Total = degreeType == "UG"
                                       ? g.Where(x => x.QuestionPartName == "B" && x.QuestionNumber >= 11 && x.QuestionNumber <= 20)
                                            .Sum(x => x.ObtainedMark)
                                       : g.Where(x => x.QuestionPartName == "B" && x.QuestionNumber >= 11 && x.QuestionNumber <= 18)
                                            .Sum(x => x.ObtainedMark),

                                   // Part C only for PG
                                   PartC_Total = degreeType == "PG"
                                       ? g.Where(x => x.QuestionPartName == "C" && x.QuestionNumber == 19).Sum(x => x.ObtainedMark)
                                       : 0,

                                   GrandTotal = g.Sum(x => x.ObtainedMark),

                                   // Include marks for each question
                                   QuestionMarks = g.ToDictionary(
                                       x => x.QuestionNumberSubNum != null
                                           ? $"{x.QuestionNumber}.{x.QuestionNumberSubNum}"
                                           : x.QuestionNumber.ToString(),
                                       x => x.ObtainedMark
                                   )
                               }).ToList();

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
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

                foreach (var item in groupedData)
                {
                    var row = new Row();
                    row.Append(CreateStringCell(item.DummyNumber.ToString()));

                    int totalQuestions = 20;
                    for (int i = 1; i <= totalQuestions; i++)
                    {
                        if (item.QuestionMarks is Dictionary<string, decimal> questionMarks)
                        {
                            questionMarks.TryGetValue(i.ToString(), out decimal mark);
                            row.Append(CreateCell(mark));
                        }
                    }

                    row.Append(CreateCell(item.PartA_Total));
                    row.Append(CreateCell(item.PartB_Total));
                    if (degreeType == "PG")
                        row.Append(CreateCell(item.PartC_Total));

                    row.Append(CreateCell(item.GrandTotal));
                    sheetData.Append(row);
                }

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }

            memoryStream.Position = 0;
            return await Task.FromResult(memoryStream);
        }

        public async Task<string?> GetInstitutionByIdAsync(long institutionId)
        {
            return await _context.Institutions
                .Where(i => i.InstitutionId == institutionId)
                .Select(i => i.Code)
                .FirstOrDefaultAsync();
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
