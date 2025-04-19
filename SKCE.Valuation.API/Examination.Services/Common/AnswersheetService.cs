﻿using Microsoft.EntityFrameworkCore;
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
            var result = await helper.GetConsolidatedItems(institutionId);
            return result;
        }

        public async Task<Boolean> SaveAnswersheetMarkAsync(AnswersheetQuestionwiseMark mark)
        {
            var markRecord = await _context.AnswersheetQuestionwiseMarks.FirstOrDefaultAsync(e => 
                e.AnswersheetId == mark.AnswersheetId && 
                e.QuestionNumber == mark.QuestionNumber && 
                e.QuestionNumberSubNum == mark.QuestionNumberSubNum);

            if (markRecord == null)
            {
                mark.IsActive = true;
                mark.CreatedDate = DateTime.Now;
                mark.ModifiedDate = DateTime.Now;

                _context.AnswersheetQuestionwiseMarks.Add(mark);
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                if (mark.ObtainedMark > 0)
                {
                    markRecord.ModifiedDate = DateTime.Now;
                    markRecord.ObtainedMark = mark.ObtainedMark;

                    await _context.SaveChangesAsync();
                    return true;
                }
                else
                {
                    _context.AnswersheetQuestionwiseMarks.Remove(markRecord);
                    await _context.SaveChangesAsync();
                    return true;
                }                
            }            
        }

    }
}
