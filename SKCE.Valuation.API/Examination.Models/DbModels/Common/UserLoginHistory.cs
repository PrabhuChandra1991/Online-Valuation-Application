using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common;

[Table("UserLoginHistory", Schema = "dbo")]

    public class UserLoginHistory:AuditModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long UserLoginHistoryId { get; set; }
    [Required]
    public long UserId { get; set; }

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string TempPassword { get; set; } = string.Empty;

    public DateTime LoginDateTime { get; set; } = DateTime.UtcNow;

    public bool IsSuccessful { get; set; }

}
