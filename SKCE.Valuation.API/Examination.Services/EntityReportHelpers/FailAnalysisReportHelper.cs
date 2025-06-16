using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Report;

namespace SKCE.Examination.Services.EntityReportHelpers
{
    public class FailAnalysisReportHelper
    {
        private readonly ExaminationDbContext _context;

        public FailAnalysisReportHelper(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FailAnalysisReportDto>> GetFailAnalysisReportData()
        {
            var examimationItems =
                await (from examination in this._context.Examinations
                       join institution in this._context.Institutions on examination.InstitutionId equals institution.InstitutionId
                       join course in this._context.Courses on examination.CourseId equals course.CourseId
                       where examination.IsActive == true
                       select new
                       {
                           ExaminationId = examination.ExaminationId,
                           InstitutionId = examination.InstitutionId,
                           InstitutionCode = institution.Code,
                           CourseId = course.CourseId,
                           CourseCode = course.Code,
                           CourseName = course.Name,
                           StudentCount = examination.StudentCount,
                           ExamType = examination.ExamType,
                       }).ToListAsync();

            var asnswersheets =
                await (from asnswersheet in this._context.Answersheets
                       join examination in this._context.Examinations
                       on asnswersheet.ExaminationId equals examination.ExaminationId
                       join course in this._context.Courses
                       on examination.CourseId equals course.CourseId
                       where asnswersheet.IsActive == true
                       select new
                       {
                           IsEvalutionCompleted = asnswersheet.IsEvaluateCompleted,
                           ExaminationId = asnswersheet.ExaminationId,
                           TotalObtainedMark = asnswersheet.TotalObtainedMark,
                           CourseCode = course.Code
                       }).ToListAsync();

            var resultItems =
              (from item in examimationItems
               group item by new { item.InstitutionId, item.CourseId } into grpItems
               select new FailAnalysisReportDto
               {
                   InstitutionId = grpItems.Key.InstitutionId,
                   InstitutionCode = grpItems.First().InstitutionCode,
                   CourseId = grpItems.Key.CourseId,
                   CourseCode = grpItems.First().CourseCode,
                   CourseName = grpItems.First().CourseName,
                   ExamType = grpItems.First().ExamType,
                   StudentTotalRegisteredCount = grpItems.Sum(x => x.StudentCount),

                   StudentTotalAppearedCount = asnswersheets
                   .Count(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId)),

                   //StudentTotalAbsentCount  --- Refer model Get property
                   PendingEvaluationCount = asnswersheets.Count(x => x.CourseCode == grpItems.First().CourseCode 
                                                                    && x.IsEvalutionCompleted == false),
                   StudentTotal_00_24_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 0 && x.TotalObtainedMark <= 24)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_25_29_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 25 && x.TotalObtainedMark <= 29)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_30_34_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 30 && x.TotalObtainedMark <= 34)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_35_39_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 35 && x.TotalObtainedMark <= 39)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_40_44_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 40 && x.TotalObtainedMark <= 44)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   //StudentTotalFailCount --- Refer model Get property

               }).ToList();

            return resultItems.OrderBy(x => x.InstitutionCode).ThenBy(x => x.CourseCode).ToList();
        }


    } // Class
}
