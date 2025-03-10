using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;

namespace SKCE.Examination.Services.Common
{
    public class CourseDepartmentService
    {
        private readonly ExaminationDbContext _context;

        public CourseDepartmentService(ExaminationDbContext context)
        {
            _context = context;
        }

        //public async Task<IEnumerable<CourseDepartmentViewModel>> GetAllCourseDepartmentsAsync()
        //{
        //    //return await _context.CourseDepartments
        //    //    .Include(cd => cd.Course)
        //    //    .Include(cd => cd.Department)
        //    //    .Select(cd => new CourseDepartmentViewModel
        //    //    {
                    
        //    //        CourseId = cd.Course.Id,
        //    //        CourseTitle = cd.Course.Title,
        //    //        CourseDescription = cd.Course.Description,
        //    //        CourseDuration = cd.Course.Duration,
        //    //        DepartmentId = cd.Department.Id,
        //    //        DepartmentName = cd.Department.Name,
        //    //        DepartmentLocation = cd.Department.Location
        //    //    })
        //    //    .ToListAsync();
        //}

    }
}

