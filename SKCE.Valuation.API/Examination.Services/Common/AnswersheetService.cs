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
        public readonly IConfiguration _configuration;

        public AnswersheetService(ExaminationDbContext context, IUserService userService, EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _userService = userService;
            _emailService = emailService;
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
            string? examYear = null, string? examMonth = null, string? examType = null,
            long? courseId = null, long? allocatedToUserId = null, long? answersheetId = null)
        {

            var helper = new AnswersheetDetailsHelper(this._context, this._configuration);
            var result = await helper.GetAnswersheetDetailsAsync(examYear, examMonth, examType, courseId, allocatedToUserId, answersheetId);
            return result;
        }

        public async Task<IEnumerable<AnswerManagementDto>> GetAnswersheetDetailsByIdAsync(long answersheetId)
        {
            var helper = new AnswersheetDetailsHelper(this._context, this._configuration);
            var result = await helper.GetAnswersheetDetailsAsync(answersheetId: answersheetId, showDummyNo: true);
            return result;
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


        public async Task<List<AnswersheetQuestionAnswerImageDto>>
            GetQuestionAndAnswerImagesByAnswersheetIdAsync(long answersheetId, int questionNumber, int questionSubNumber)
        {
            var helper = new AnswersheetQuestionAnswerImageHelper(this._context);
            var result = await helper.GetQuestionAndAnswerImagesByAnswersheetId(answersheetId, questionNumber, questionSubNumber);
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

            var answersheetIds = await helper.AllocateAnswersheetsToUserRandomly(inputData, loggedInUserId);

            if (answersheetIds.Any())
            {
                var user = await _userService.GetUserOnlyByIdAsync(inputData.UserId);
                if (user != null)
                {
                    var emailHelper = new AnswersheetAllocateMailHelper(
                        this._context, this._emailService, _configuration["LoginUrl"].ToString());

                    await emailHelper.SendAllocatedEmail(answersheetIds, user.Email);
                }
            }

            return true;
        }

        public async Task<(MemoryStream, string)> ExportMarksAsync(
            long institutionId, long courseId, string examYear, string examMonth, string examType)
        {
            var helper = new AnswersheetExportMarkHelper(this._context);
            var result = await helper.ExportMarksAsync(institutionId, courseId, examYear, examMonth, examType);
            return result;
        }


        private static Cell CreateCell(decimal value)
        {
            return new Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(value)
            };
        }

        public async Task<bool> RevertEvaluation(long answersheetId)
        {
            var helper = new AnswersheetMarkTransHelper(this._context);
            return await helper.RevertEvaluation(answersheetId);
        }

        public async Task<bool> EvaluationHistory(long answersheetId, long questionNumber)
        {
            var helper = new AnswersheetMarkTransHelper(this._context);
            return await helper.EvaluationHistory(answersheetId,questionNumber);
        }
    }
}
