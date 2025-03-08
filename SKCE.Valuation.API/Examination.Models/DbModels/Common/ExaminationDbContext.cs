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
        public DbSet<Course> Courses { get; set; }
        public DbSet<QPTag> QPTags { get; set; }
        public DbSet<QPTemplate> QPTemplates { get; set; }
        public DbSet<QPTemplateTag> QPTemplateTagDetails { get; set; }
        public DbSet<UserQPTemplate> QPTemplateTagByUserDetails { get; set; }
        public DbSet<UserQPTemplateTag> UserQPTemplateTags { get; set; }
        public DbSet<QPTemplateStatusType> QPTemplateStatusTypes { get; set; }
        public DbSet<Document> DocumentDetails { get; set; }
        public DbSet<DegreeType> DegreeTypes { get; set; }
        public DbSet<ExamMonth> ExamMonths { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserCourse>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.UserCourses)
            .HasForeignKey(uc => uc.UserId);

            modelBuilder.Entity<UserAreaOfSpecialization>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserAreaOfSpecializations)
                .HasForeignKey(us => us.UserId);
        }
    }
}
