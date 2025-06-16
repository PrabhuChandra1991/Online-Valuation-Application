using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Report;

namespace SKCE.Examination.Services.EntityReportHelpers
{
    public class PassAnalysisReportHelper
    {
        private readonly ExaminationDbContext _context;

        public PassAnalysisReportHelper(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PassAnalysisReportDto>> GetPassAnalysisReportData()
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
               select new PassAnalysisReportDto
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
                   StudentTotal_45_50_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 45 && x.TotalObtainedMark <= 50)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_51_60_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 51 && x.TotalObtainedMark <= 60)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_61_70_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 61 && x.TotalObtainedMark <= 70)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_71_80_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 71 && x.TotalObtainedMark <= 80)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_81_90_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 81 && x.TotalObtainedMark <= 90)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   StudentTotal_91_100_Count = asnswersheets
                   .Where(x => x.TotalObtainedMark >= 91 && x.TotalObtainedMark <= 100)
                   .Where(x => grpItems.Select(y => y.ExaminationId).Contains(x.ExaminationId))
                   .Count(),

                   //StudentTotalPassCount --- Refer model Get property

               }).ToList();

            return resultItems.OrderBy(x => x.InstitutionCode).ThenBy(x => x.CourseCode).ToList();
        }


    } // Class
}
