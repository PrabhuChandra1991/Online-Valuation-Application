using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return await _context.Courses.ToListAsync();
        }

        public async Task<Course?> GetCourseByIdAsync(int id)
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
