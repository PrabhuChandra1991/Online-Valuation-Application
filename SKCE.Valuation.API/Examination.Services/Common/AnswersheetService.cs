using DocumentFormat.OpenXml.InkML;
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
        public Task<MemoryStream> ExportMarksAsync(long institutionId, string examYear, string examMonth,  string degreeType) 
        {
            //var data = (from e in _context.Examinations
            //            join a in _context.Answersheets on e.ExaminationId equals a.ExaminationId
            //            join qm in _context.AnswersheetQuestionwiseMarks on a.AnswersheetId equals qm.AnswersheetId
            //            where e.InstitutionId == institutionId &&
            //                  e.ExamYear == examYear &&
            //                  e.ExamMonth == examMonth &&
            //                  e.DegreeType == degreeType
            //            group qm by new { a.DummyNumber, e.DegreeType } into g
            //            select new
            //            {
            //                DummyNumber = g.Key.DummyNumber,
            //                DegreeType = g.Key.DegreeType,

            //                // Part A: Questions 1–10
            //                PartA_Total = g.Where(x => x.Part == "A" && x.QuestionNumber >= 1 && x.QuestionNumber <= 10)
            //                               .Sum(x => x.Marks),

            //                // Part B
            //                PartB_Total = g.Key.DegreeType == "UG"
            //                    ? g.Where(x => x.Part == "B" && x.QuestionNumber >= 11 && x.QuestionNumber <= 20)
            //                         .Sum(x => x.Marks)
            //                    : g.Where(x => x.Part == "B" && x.QuestionNumber >= 11 && x.QuestionNumber <= 18)
            //                         .Sum(x => x.Marks),

            //                // Part C only for PG
            //                PartC_Total = g.Key.DegreeType == "PG"
            //                    ? g.Where(x => x.Part == "C" && x.QuestionNumber == 19).Sum(x => x.Marks)
            //                    : 0,

            //                GrandTotal = g.Sum(x => x.Marks),

            //                // Include marks for each question
            //                QuestionMarks = g.ToDictionary(
            //                    x => x.SubPart != null
            //                        ? $"{x.QuestionNumber}.{x.SubPart}"
            //                        : x.QuestionNumber.ToString(),
            //                    x => x.Marks
            //                )
            //            }).ToList();

            //return ExcelExportHelper.GenerateExcel(data, degreeType);
            return Task.FromResult(new MemoryStream());
        }

        public async Task<string?> GetInstitutionByIdAsync(long institutionId)
        {
            return await _context.Institutions
                .Where(i => i.InstitutionId == institutionId)
                .Select(i => i.Code)
                .FirstOrDefaultAsync();
        }
    }
}
