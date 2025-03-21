using SKCE.Examination.Models.DbModels.QPSettings;
using Microsoft.EntityFrameworkCore;

namespace SKCE.Examination.Models.DbModels.Common
{
    public class ExaminationDbContext : DbContext
    {
        public ExaminationDbContext(DbContextOptions<ExaminationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
        public DbSet<UserCourse> UserCourses { get; set; }
        public DbSet<UserAreaOfSpecialization> UserSpecializations { get; set; }
        public DbSet<UserDesignation> UserDesignations { get; set; }
        public DbSet<UserQualification> UserQualifications { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<QPDocument> QPDocuments { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSyllabusDocument> CourseSyllabusDocuments { get; set; }
        public DbSet<CourseDepartment> CourseDepartments { get; set; }
        public DbSet<QPTag> QPTags { get; set; }
        public DbSet<QPTemplateStatusType> QPTemplateStatusTypes { get; set; }
        public DbSet<QPTemplate> QPTemplates { get; set; }
        public DbSet<QPTemplateDocument> QPTemplateDocuments { get; set; }
        public DbSet<QPTemplateInstitution> QPTemplateInstitutions { get; set; }
        public DbSet<QPTemplateInstitutionDocument> QPTemplateInstitutionDocuments { get; set; }
        public DbSet<QPTemplateInstitutionDepartment> QPTemplateInstitutionDepartments { get; set; }
        public DbSet<UserQPTemplate> UserQPTemplates { get; set; }
        public DbSet<UserQPTemplateDocument> UserQPTemplateDocuments { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DegreeType> DegreeTypes { get; set; }
        public DbSet<ExamMonth> ExamMonths { get; set; }
        public DbSet<ImportHistory> ImportHistories { get; set; }
        public DbSet<QPDocumentBookMark> QPDocumentBookMarks { get; set; }
        public DbSet<UserQPDocumentBookMark> UserQPDocumentBookMarks { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
