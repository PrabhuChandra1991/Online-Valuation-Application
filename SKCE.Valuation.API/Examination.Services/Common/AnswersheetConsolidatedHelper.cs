using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.Services.Common
{
    public class AnswersheetConsolidatedHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public AnswersheetConsolidatedHelper(ExaminationDbContext context) => _dbContext = context;

        public async Task<List<AnswersheetConsolidatedDto>> GetConsolidatedItems(long institutionId)
        {

            var resultItems = new List<AnswersheetConsolidatedDto>();

            var examItems =
                await (from exam in this._dbContext.Examinations
                       join institution in this._dbContext.Institutions on exam.InstitutionId equals institution.InstitutionId
                       join course in this._dbContext.Courses on exam.CourseId equals course.CourseId
                       join dept in this._dbContext.Departments on exam.DepartmentId equals dept.DepartmentId
                       join degType in this._dbContext.DegreeTypes on exam.DegreeTypeId equals degType.DegreeTypeId

                       where exam.InstitutionId == institutionId && exam.IsActive == true

                       select new
                       {
                           exam.ExaminationId,
                           InstitutionCode = institution.Code,
                           CourseId = course.CourseId,
                           CourseCode = course.Code,
                           DepartmentName = dept.ShortName,
                           exam.RegulationYear,
                           exam.BatchYear,
                           DegreeTypeId = exam.DegreeTypeId,
                           DegreeName = degType.Name,
                           exam.ExamType,
                           exam.Semester,
                           exam.ExamMonth,
                           exam.ExamYear,
                           exam.StudentCount
                       }).ToListAsync();

            var answersheetItems = this._dbContext.Answersheets.Where(x => x.InstitutionId == institutionId && x.IsActive).ToList();

            foreach (var item in examItems)
            {
                var newItem = new AnswersheetConsolidatedDto
                {
                    ExaminationId = item.ExaminationId,
                    InstitutionCode = item.InstitutionCode,
                    CourseCode = item.CourseCode,
                    DepartmentShortName = item.DepartmentName,
                    RegulationYear = item.RegulationYear,
                    BatchYear = item.BatchYear,
                    DegreeType = item.DegreeName,
                    ExamType = item.ExamType,
                    Semester = (int)item.Semester,
                    ExamMonth = item.ExamMonth,
                    ExamYear = item.ExamYear,
                    StudentTotalCount = item.StudentCount,
                    AnswerSheetTotalCount = 0,
                    AnswerSheetAllocatedCount = 0,
                    AnswerSheetNotAllocatedCount = 0
                };

                var selectedItems = answersheetItems
                                              .Where(x =>
                                                  x.InstitutionId == institutionId
                                                  && x.CourseId == item.CourseId
                                                  && x.BatchYear == item.BatchYear
                                                  && x.RegulationYear == item.RegulationYear
                                                  && x.Semester == item.Semester
                                                  && x.DegreeTypeId == item.DegreeTypeId
                                                  && x.ExamType == item.ExamType
                                                  && x.ExamMonth == item.ExamMonth
                                                  && x.ExamYear == item.ExamYear).ToList();

                newItem.AnswerSheetTotalCount = selectedItems.Count();
                newItem.AnswerSheetAllocatedCount = selectedItems.Where(x => x.AllocatedToUserId != null).Count();
                newItem.AnswerSheetNotAllocatedCount = selectedItems.Where(x => x.AllocatedToUserId == null).Count();

                resultItems.Add(newItem);

            }



            return resultItems;

        }



    } // Class
}
