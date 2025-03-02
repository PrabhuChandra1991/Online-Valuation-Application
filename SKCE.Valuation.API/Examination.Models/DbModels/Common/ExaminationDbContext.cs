using SKCE.Examination.Models.DbModels.QPSettings;
using Microsoft.EntityFrameworkCore;

namespace SKCE.Examination.Models.DbModels.Common
{
    public class ExaminationDbContext : DbContext
    {
        public ExaminationDbContext(DbContextOptions<ExaminationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Departments> Departments { get; set; }
        public DbSet<Designations> Designations { get; set; }
        public DbSet<Institutions> Institutions { get; set; }
        public DbSet<CourseDetails> CourseDetails { get; set; }
        public DbSet<QPTags> QPTags { get; set; }
        public DbSet<QPTemplates> QPTemplates { get; set; }
        public DbSet<QPTemplateTagDetails> QPTemplateTagDetails { get; set; }
        public DbSet<QPTemplateTagByUserDetails> QPTemplateTagByUserDetails { get; set; }
        public DbSet<DocumentDetails> DocumentDetails { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
