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

        public async Task<List<AnswersheetConsolidatedDto>> GetConsolidatedItems(long institutionId)
        {
            var resultItems = await (from asheet in _dbContext.Answersheets
                                     join exam in _dbContext.Examinations on asheet.ExaminationId equals exam.ExaminationId
                                     join institution in _dbContext.Institutions on exam.InstitutionId equals institution.InstitutionId
                                     join course in _dbContext.Courses on exam.CourseId equals course.CourseId
                                     join dept in _dbContext.Departments on exam.DepartmentId equals dept.DepartmentId
                                     join degType in _dbContext.DegreeTypes on exam.DegreeTypeId equals degType.DegreeTypeId
                                     where exam.InstitutionId == institutionId && exam.IsActive == true
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
                                         StudentTotalCount = exam.StudentCount,
                                         AnswerSheetTotalCount = exam.Answersheets.Count(x => x.IsActive),
                                         AnswerSheetAllocatedCount = exam.Answersheets.Count(x => x.IsActive && x.AllocatedToUserId != null),
                                         AnswerSheetNotAllocatedCount = exam.Answersheets.Count(x => x.IsActive && x.AllocatedToUserId == null),
                                     }).ToListAsync(); 
            return resultItems;
        }



    } // Class
}
