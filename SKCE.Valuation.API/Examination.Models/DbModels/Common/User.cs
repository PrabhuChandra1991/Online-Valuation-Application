using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Examination.Models.DbModels.Common
{
    [Table("Users", Schema = "dbo")]
    public class User: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        [Required, EmailAddress]
        public required string Email { get; set; }

        [MaxLength(15)]
        public string? MobileNumber { get; set; }

        public int? RoleId { get; set; } 

        public int? Experience { get; set; }

        public int? DepartmentId { get; set; } 

        public int? DesignationId { get; set; }

        public string? CollegeName { get; set; }

        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankBranchName { get; set; }
        public string? BankIFSCCode { get; set; }
        public bool IsEnabled { get; set; }
    }
}
