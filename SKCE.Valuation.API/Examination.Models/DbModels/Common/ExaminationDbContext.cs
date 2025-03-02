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
        }
    }
}
