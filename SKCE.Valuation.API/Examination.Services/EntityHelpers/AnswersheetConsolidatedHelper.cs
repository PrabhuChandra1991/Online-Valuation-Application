using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.Services.EntityHelpers
{
    public class AnswersheetConsolidatedHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public AnswersheetConsolidatedHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }

        public async Task<List<AnswersheetConsolidatedDto>> GetConsolidatedItems(
            string examYear, string examMonth, string examType)
        {
            var resultItems = new List<AnswersheetConsolidatedDto>();

            var qryItems = await (from exam in _dbContext.Examinations
                                  join course in _dbContext.Courses on exam.CourseId equals course.CourseId

                                  where exam.IsActive == true
                                  && exam.ExamYear == examYear
                                  && exam.ExamMonth == examMonth
                                  && exam.ExamType == examType

                                  select new
                                  {
                                      CourseId = course.CourseId,
                                      CourseCode = course.Code,
                                      CourseName = course.Name,
                                      StudentTotalCount = exam.StudentCount,
                                      AnswerSheetTotalCount = exam.Answersheets.Count(x => x.IsActive),
                                      AnswerSheetAllocatedCount = exam.Answersheets.Count(x => x.IsActive && x.AllocatedToUserId != null),
                                      AnswerSheetNotAllocatedCount = exam.Answersheets.Count(x => x.IsActive && x.AllocatedToUserId == null),
                                  }).ToListAsync();

            var grpItems = qryItems.GroupBy(x => x.CourseId);

            foreach (var item in grpItems)
            {
                resultItems.Add(new AnswersheetConsolidatedDto
                {
                    CourseId = item.First().CourseId,
                    CourseCode = item.First().CourseCode,
                    CourseName = item.First().CourseName,
                    StudentTotalCount = item.Sum(x => x.StudentTotalCount),
                    AnswerSheetTotalCount = item.Sum(x => x.AnswerSheetTotalCount),
                    AnswerSheetAllocatedCount = item.Sum(x => x.AnswerSheetAllocatedCount),
                    AnswerSheetNotAllocatedCount = item.Sum(x => x.AnswerSheetNotAllocatedCount),
                });
            }

            return resultItems.OrderBy(x => x.CourseCode).ToList();
        }


    } // Class
}
