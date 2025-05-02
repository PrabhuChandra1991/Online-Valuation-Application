using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.QPSettings;

namespace SKCE.Examination.Services.Common
{
    public class CourseService
    {
        private readonly ExaminationDbContext _context;

        public CourseService(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Course>> GetAllCoursesAsync()
        {
            var courses = await _context.Courses.ToListAsync();
            var finalCourseList = new List<Course>();
            var qpTemplates = _context.QPTemplates.ToList();
            foreach (var course in courses)
            {
                var qpTemplate = qpTemplates.FirstOrDefault(qpt => qpt.CourseId == course.CourseId);
                if (qpTemplate != null)
                {
                    var userQPTemplates = _context.UserQPTemplates.Where(uqpt => uqpt.QPTemplateId == qpTemplate.QPTemplateId && uqpt.IsActive).ToList();
                    if (userQPTemplates.Count < 2)
                    {
                        if (!finalCourseList.Contains(course))
                            finalCourseList.Add(course);
                        continue;
                    }
                }
                else
                {
                    finalCourseList.Add(course);
                }
            }
            return courses.OrderByDescending(x => x.Code).ToList();
        }

        public async Task<Course?> GetCourseByIdAsync(long id)
        {
            return await _context.Courses.FindAsync(id);
        }
        
        public async Task<Course?> AddCourseAsync(Course course)
        {
            AuditHelper.SetAuditPropertiesForInsert(course, course.CreatedById);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        public async Task<bool> UpdateCourseAsync(int id, Course updatedCourse)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return false;

            course.Code = updatedCourse.Code;
            course.Name = updatedCourse.Name;
            AuditHelper.SetAuditPropertiesForUpdate(course, updatedCourse.ModifiedById);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return false;
            AuditHelper.SetAuditPropertiesForInsert(course, course.ModifiedById);
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
