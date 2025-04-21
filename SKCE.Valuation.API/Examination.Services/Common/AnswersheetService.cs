using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.EntityHelpers;
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

        public AnswersheetService(ExaminationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
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
            long? institutionId = null, long? courseId = null, long? allocatedToUserId = null)
        {
            var resultItems =
                await (from answersheet in _context.Answersheets
                       join institute in _context.Institutions on answersheet.InstitutionId equals institute.InstitutionId
                       join course in _context.Courses on answersheet.CourseId equals course.CourseId
                       join dtype in _context.DegreeTypes on answersheet.DegreeTypeId equals dtype.DegreeTypeId

                       join allocatedUser in this._context.Users on answersheet.AllocatedToUserId equals allocatedUser.UserId into allocatedUserResults
                       from allocatedUserResult in allocatedUserResults.DefaultIfEmpty()

                       where
                       answersheet.IsActive == true
                       && (answersheet.InstitutionId == institutionId || institutionId == null)
                       && (answersheet.CourseId == courseId || courseId == null)
                       && (answersheet.AllocatedToUserId == allocatedToUserId || allocatedToUserId == null)

                       select new AnswerManagementDto
                       {
                           AnswersheetId = answersheet.AnswersheetId,
                           InstitutionId = answersheet.InstitutionId,
                           InstitutionName = institute.Name,
                           RegulationYear = answersheet.RegulationYear,
                           BatchYear = answersheet.BatchYear,
                           DegreeTypeName = dtype.Name,
                           ExamType = answersheet.ExamType,
                           Semester = answersheet.Semester,
                           CourseId = answersheet.CourseId,
                           CourseCode = course.Code,
                           CourseName = course.Name,
                           ExamMonth = answersheet.ExamMonth,
                           ExamYear = answersheet.ExamYear,
                           DummyNumber = answersheet.DummyNumber,
                           UploadedBlobStorageUrl = answersheet.UploadedBlobStorageUrl,
                           AllocatedUserName = (allocatedUserResult != null ? allocatedUserResult.Name : string.Empty)
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

        public async Task<Boolean> SaveAnswersheetMarkAsync(AnswersheetQuestionwiseMark entity)
        {
            var helper = new AnswersheetMarkTransHelper(this._context);
            return await helper.SaveAnswersheetMarkAsync(entity);
        }

        public async Task<Boolean> AllocateAnswerSheetsToUser(AnswersheetAllocateInputModel inputData)
        {
            long loggedInUserId = 1;
            var helper = new AnswersheetAllocateHelper(this._context);
            var response = await helper.AllocateAnswersheetsToUserRandomly(inputData, loggedInUserId);
            if (response)
            {
                var user = await _userService.GetUserByIdAsync(loggedInUserId);
                _emailService.SendEmailAsync(user.Email, "SKCE Online Examination Platform: Answeersheets allocated", $"Hi {user.Name},\n\nYou have been allocated with {inputData.Noofsheets} answersheets for Evaluation.\n\n Please Evaluate. \n\n").Wait();
            }
            return response;
        }        

    }
}
