using Microsoft.EntityFrameworkCore.Metadata;
using SKCE.Examination.Models.DbModels.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("User", Schema = "dbo")]
    public class User: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserId { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        [Required, EmailAddress]
        public required string Email { get; set; }

        [MaxLength(15)]
        public required string MobileNumber { get; set; }
        public long? RoleId { get; set; }
        public  string? Qualification { get; set; }
        public long? WorkExperience { get; set; }
        public long? DepartmentId { get; set; } 
        public long? DesignationId { get; set; }
        public string? CollegeName { get; set; }
        public  string? AreaOfSpecialization { get; set; }
        public long CourseId { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankBranchName { get; set; }
        public string? BankIFSCCode { get; set; }
        public bool IsEnabled { get; set; }
    }
}
