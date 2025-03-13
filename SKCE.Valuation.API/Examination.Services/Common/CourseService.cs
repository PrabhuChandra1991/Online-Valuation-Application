using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Common;

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
        public async Task<Course?> GetCourseByIdAsync(long id)
        {
            return await _context.Courses.FindAsync(id);
        }
        public async Task<QPTemplateVM?> GetQPTemplateByCourseId(long courseId)
        {
            var course = await _context.Courses.Where(cd => cd.CourseId == courseId)
                .Select(c => new QPTemplateVM
                {
                    CourseId = c.CourseId,
                    CourseCode = c.Code,
                    CourseName = c.Name,
                    Institutions = new List<InstitutionDepartmentVM>()
                }).FirstOrDefaultAsync();

            if (course == null) return null;

            var courseDepartments = _context.CourseDepartments.Where(cd => cd.CourseId == courseId).ToList();
            if (courseDepartments.Any())
            {
                course.RegulationYear = courseDepartments.First().RegulationYear;
                course.BatchYear = courseDepartments.First().BatchYear;
                course.ExamYear = courseDepartments.First().ExamYear;
                course.ExamMonth = courseDepartments.First().ExamMonth;
                course.ExamType = courseDepartments.First().ExamType;
                course.Semester = courseDepartments.First().Semester;
                course.TotalStudentCount = courseDepartments.Sum(cd => cd.StudentCount);
            }
            AuditHelper.SetAuditPropertiesForInsert(course, 1);
            course.qpDocumentVMs = new List<QPDocumentVM>() { 
            // add QP Template document for Course Syllabus document
           new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentName = "", QPDocumentTypeId = 1, QPDocumentTypeName = "Course Syllabus document", QPTemplateId = course.QPTemplateId },

            // add QP Template document for Expert Preview with QP and Answer Generation with  bookmark
            new QPDocumentVM() { QPDocumentId=0,DocumentId=0,QPDocumentName="",QPDocumentTypeId=2,QPDocumentTypeName="Expert preview document",QPTemplateId=course.QPTemplateId},

            // add QP Template document for Expert use for QP and Answer Generation with  bookmark
            new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentName = "", QPDocumentTypeId = 3, QPDocumentTypeName = "Expert QP generation", QPTemplateId = course.QPTemplateId }
            };
            foreach (var qpDocumentVM in course.qpDocumentVMs)
            {
                AuditHelper.SetAuditPropertiesForInsert(qpDocumentVM, 1);
            }
            List<long> institutionIds = courseDepartments.Select(cd => cd.InstitutionId).Distinct().ToList();
            var institutions = _context.Institutions.ToList();
            var departments = _context.Departments.ToList();

            foreach (var institutionId in institutionIds)
            {
                var institution = institutions.FirstOrDefault(i => i.InstitutionId == institutionId);
                if (institution != null)
                {
                    var institutionVM = new InstitutionDepartmentVM
                    {
                        InstitutionId = institution.InstitutionId,
                        InstitutionName = institution.Name,
                        InstitutionCode = institution.Code,
                        TotalStudentCount = courseDepartments.Where(i => i.InstitutionId == institution.InstitutionId).Sum(ci => ci.StudentCount),
                        Departments = new List<DepartmentVM>()
                    };
                    var departmentVMs = courseDepartments.Join(departments, cd => cd.DepartmentId, d => d.DepartmentId, (cd, d) => new { cd, d })
                        .Where(cd => cd.cd.InstitutionId == institution.InstitutionId)
                        .Select(cd => new DepartmentVM
                        {
                            DepartmentId = cd.d.DepartmentId,
                            DepartmentName = cd.d.Name,
                            DepartmentShortName = cd.d.ShortName,
                            StudentCount = cd.cd.StudentCount
                        }).ToList();
                    institutionVM.Departments = departmentVMs;

                    institutionVM.qpDocumentVMs = new List<QPDocumentVM>() { 
                    // add QP Template document for Print with QP With  Tags
                    new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentName = "", QPDocumentTypeId = 4, QPDocumentTypeName = "Print preview QP document", QPTemplateId = course.QPTemplateId },

                    // add QP Template document for Print with QP and Answer With Tags
                    new QPDocumentVM() { QPDocumentId = 0, DocumentId = 0, QPDocumentName = "", QPDocumentTypeId = 5, QPDocumentTypeName = "Print preview QP Answer document", QPTemplateId = course.QPTemplateId }
                    };
                    foreach (var qpDocumentVM in institutionVM.qpDocumentVMs)
                    {
                        AuditHelper.SetAuditPropertiesForInsert(qpDocumentVM, 1);
                    }
                    course.Institutions.Add(institutionVM);
                }
            }
            return course;
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
