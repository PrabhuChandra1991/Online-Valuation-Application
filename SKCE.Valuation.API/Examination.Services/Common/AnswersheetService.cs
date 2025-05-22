using Amazon.Runtime.EventStreams.Internal;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.EntityHelpers;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class AnswersheetService
    {
        private readonly ExaminationDbContext _context;
        private readonly IUserService _userService;
        private readonly EmailService _emailService;
        //-------------
        public readonly string _blobBaseUrl;
        public readonly string _containerName;
        //
        public readonly IConfiguration _configuration;

        public AnswersheetService(ExaminationDbContext context, IUserService userService, EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _userService = userService;
            _emailService = emailService;
            _blobBaseUrl = configuration["AzureBlobStorage:BaseUrl"]; ;
            _containerName = configuration["AzureBlobStorage:ContainerName"];
            _configuration = configuration;
        }

        public async Task<IEnumerable<CourseWithAnswersheet>> GetCoursesHavingAnswersheetAsync(string examYear, string examMonth, string examType, long institutionId = 0)
        {
            try
            {
                var courses = await (
                from a in _context.Answersheets
                join e in _context.Examinations on a.ExaminationId equals e.ExaminationId
                join c in _context.Courses on e.CourseId equals c.CourseId
                where e.ExamYear == examYear && e.ExamMonth == examMonth && e.ExamType == examType
                && (institutionId == 0 || e.InstitutionId == institutionId)
                group new { a, c } by new
                {
                    c.CourseId,
                    c.Code,
                    c.Name
                } into g
                select new CourseWithAnswersheet
                {
                    CourseId = g.Key.CourseId,
                    Code = g.Key.Code,
                    Name = g.Key.Name,
                    Count = g.Count()
                }
                ).OrderBy(x => x.Code).ToListAsync();

                return courses;
            }
            catch (Exception ex)
            {
                throw;
            }
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
            string? examYear = null, string? examMonth = null, string? examType = null, long? courseId = null,
            long? allocatedToUserId = null, long? answersheetId = null)
        {
            try
            {
                var resultItems = await (from answersheet in _context.Answersheets

                                         join examination in this._context.Examinations
                                         on answersheet.ExaminationId equals examination.ExaminationId

                                         join institute in _context.Institutions
                                         on examination.InstitutionId equals institute.InstitutionId

                                         join course in _context.Courses
                                         on examination.CourseId equals course.CourseId

                                         join dtype in _context.DegreeTypes
                                         on examination.DegreeTypeId equals dtype.DegreeTypeId

                                         join allocatedUser in this._context.Users
                                         on answersheet.AllocatedToUserId equals allocatedUser.UserId into allocatedUserResults
                                         from allocatedUserResult in allocatedUserResults.DefaultIfEmpty()

                                         where
                                         answersheet.IsActive == true
                                         && (examination.ExamYear == examYear || examYear == null)
                                         && (examination.ExamMonth == examMonth || examMonth == null)
                                         && (examination.ExamType == examType || examType == null)
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
                                             UploadedBlobStorageUrl = null,
                                             AllocatedUserName = (allocatedUserResult != null ? allocatedUserResult.Name : string.Empty),
                                             TotalObtainedMark = answersheet.TotalObtainedMark,
                                             IsEvaluateCompleted = answersheet.IsEvaluateCompleted
                                         }).ToListAsync();


                foreach (var item in resultItems)
                {
                    var dummyNo = item.DummyNumber.Trim();
                    item.UploadedBlobStorageUrl = GetDummyNumberBlobStorageUrl(item.CourseCode, dummyNo);
                    if (allocatedToUserId != null || answersheetId != null)
                    {
                        item.DummyNumber = GetDummyNumberMasked(dummyNo.Trim());
                    }
                }

                return resultItems.OrderByDescending(x => x.TotalObtainedMark).OrderBy(x => x.IsEvaluateCompleted).ToList();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string GetDummyNumberMasked(string dummyNo)
        {
            return string.Concat(new String('x', 10), dummyNo.AsSpan(dummyNo.Length - 6));
        }

        private string GetDummyNumberBlobStorageUrl(string courseCode, string dummyNumber)
        {
            courseCode = courseCode.Replace('/', '_');

            //Sample url
            //https://skceuatdocuments.blob.core.windows.net/skcedocumentcontainerdev/ANSWERSHEET/23AD201/833825040813032020q52224315223.pdf

            return $"{this._blobBaseUrl}/{this._containerName}/ANSWERSHEET/{courseCode}/{dummyNumber}.pdf";
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

        public async Task<List<AnswersheetConsolidatedDto>> GetExamConsolidatedAnswersheetsAsync(
            string examYear, string examMonth, string examType)
        {
            var helper = new AnswersheetConsolidatedHelper(this._context);
            return await helper.GetConsolidatedItems(examYear, examMonth, examType);
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

        public async Task<bool> GetAnswerSheetAvailable(long answersheetId)
        {
            var result =
              await (from answer in this._context.Answersheets
                     join exam in this._context.Examinations on answer.ExaminationId equals exam.ExaminationId
                     join course in this._context.Courses on exam.CourseId equals course.CourseId
                     where answer.AnswersheetId == answersheetId
                     select new
                     {
                         course.Code,
                         answer.DummyNumber
                     }).FirstOrDefaultAsync();

            if (result == null)
                return false;

            var fileLocation = "ANSWERSHEET/" + result.Code.Replace('/', '_') + "/" + result.DummyNumber.ToString() + ".pdf";

            var helper = new BlobStorageHelper(this._configuration);
            return await helper.ExistsAsync(fileLocation);
        }

        public async Task<Boolean> AllocateAnswerSheetsToUser(AnswersheetAllocateInputModel inputData)
        {
            if (inputData.UserId == 0 || inputData.Noofsheets == 0)
                return false;

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
                        "SKCE Online Examination Platform: Answeersheets allocated", $"Hi {user.Name},\n\nYou have been allocated with {inputData.Noofsheets} answersheets for Evaluation.\n\n Please Evaluate. \n\n");
                }
            }

            return true;
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
