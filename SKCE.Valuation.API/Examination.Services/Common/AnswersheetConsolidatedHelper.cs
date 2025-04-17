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
            var resultItems =
                await (from exam in this._dbContext.Examinations
                       join institution in this._dbContext.Institutions on exam.InstitutionId equals institution.InstitutionId
                       join course in this._dbContext.Courses on exam.CourseId equals course.CourseId
                       join dept in this._dbContext.Departments on exam.DepartmentId equals dept.DepartmentId
                       join degType in this._dbContext.DegreeTypes on exam.DegreeTypeId equals degType.DegreeTypeId

                       where exam.InstitutionId == institutionId
                       && exam.IsActive == true

                       select new AnswersheetConsolidatedDto
                       {
                           ExaminationId = exam.ExaminationId,
                           InstitutionCode = institution.Code,
                           CourseCode = course.Code,
                           DepartmentShortName = dept.ShortName,
                           RegulationYear = exam.RegulationYear,
                           BatchYear = exam.BatchYear,
                           DegreeType = degType.Name,
                           ExamType = exam.ExamType,
                           Semester = (int)exam.Semester,
                           ExamMonth = exam.ExamMonth,
                           ExamYear = exam.ExamYear,
                           StudentCount = exam.StudentCount,
                           DummyNumberCount = this._dbContext.Answersheets
                                               .Where(x =>
                                                   x.InstitutionId == exam.InstitutionId
                                                   && x.CourseId == exam.CourseId
                                                   && x.BatchYear == exam.BatchYear
                                                   && x.RegulationYear == exam.RegulationYear
                                                   && x.Semester == exam.Semester
                                                   && x.DegreeTypeId == exam.DegreeTypeId
                                                   && x.ExamType == exam.ExamType && x.IsActive).Count()
                       }).ToListAsync();

            return resultItems;

        }



    } // Class
}
