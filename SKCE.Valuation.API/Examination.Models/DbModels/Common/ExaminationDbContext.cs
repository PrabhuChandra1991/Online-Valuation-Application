using Examination.Models.DbModels.Common;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Examination.Models.DBModels.Common
{
    public class ExaminationDbContext : DbContext
    {
        public ExaminationDbContext(DbContextOptions<ExaminationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User {Id=1, Email= "superadmin@skcet.ac.in",Name= "super admin" });
            // Seed Data for Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Expert" },
                new Role { Id = 3, Name = "Scan Processor" }
            );
        }
    }
}
