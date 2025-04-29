using SKCE.Examination.Models.DbModels.QPSettings;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Serialization;

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
        public DbSet<CourseSyllabusMaster> CourseSyllabusMasters { get; set; }
        public DbSet<CourseSyllabusDocument> CourseSyllabusDocuments { get; set; }
        public DbSet<Examination> Examinations { get; set; }
        public DbSet<QPTag> QPTags { get; set; }
        public DbSet<QPTemplateStatusType> QPTemplateStatusTypes { get; set; }
        public DbSet<QPTemplate> QPTemplates { get; set; }
        public DbSet<UserQPTemplate> UserQPTemplates { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DegreeType> DegreeTypes { get; set; }
        public DbSet<ExamMonth> ExamMonths { get; set; }
        public DbSet<ImportHistory> ImportHistories { get; set; }
        public DbSet<QPDocumentBookMark> QPDocumentBookMarks { get; set; }
        public DbSet<UserQPDocumentBookMark> UserQPDocumentBookMarks { get; set; }
        public DbSet<AnswersheetImportHistory> AnswersheetImportHistories { get; set; }
        public DbSet<Answersheet> Answersheets { get; set; }
        public DbSet<AnswersheetQuestionwiseMark> AnswersheetQuestionwiseMarks { get; set; }
        public DbSet<AnswersheetImport> AnswersheetImports { get; set; }
        public DbSet<AnswersheetImportDetail> AnswersheetImportDetails { get; set; }
        public DbSet<SelectedQPDetail> SelectedQPDetails { get; set; }
        public DbSet<SelectedQPBookMarkDetail> SelectedQPBookMarkDetails { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }



        public override int SaveChanges()
        {
            foreach (var entry in ChangeTracker.Entries<User>())
            {
                entry.Property(u => u.Mode).IsModified = false; // Ignore Mode
            }
            return base.SaveChanges();
        }
    }
}
