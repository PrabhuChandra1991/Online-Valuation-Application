using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
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

        public AnswersheetService(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Answersheet>> GetAllAnswersheetsAsync()
        {
            return await _context.Answersheets.ToListAsync();
        }

        public async Task<IEnumerable<AnswerManagementDto>> GetAnswersheetDetailsAsync(
            long? institutionId = null, long? courseId = null, long? allocatedToUserId = null)
        {
            var resultItems =
                await (from answersheet in _context.Answersheets
                       join institute in _context.Institutions on answersheet.InstitutionId equals institute.InstitutionId
                       join course in _context.Courses on answersheet.CourseId equals course.CourseId
                       join dtype in _context.DegreeTypes on answersheet.DegreeTypeId equals dtype.DegreeTypeId

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
                           ExampType = answersheet.ExamYear,
                           Semester = answersheet.Semester,
                           CourseId = answersheet.CourseId,
                           CourseCode = course.Code,
                           CourseName = course.Name,
                           ExamMonth = answersheet.ExamMonth,
                           ExamYear = answersheet.ExamYear,
                           DummyNumber = answersheet.DummyNumber,
                           UploadedBlobStorageUrl = answersheet.UploadedBlobStorageUrl
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

    }
}
