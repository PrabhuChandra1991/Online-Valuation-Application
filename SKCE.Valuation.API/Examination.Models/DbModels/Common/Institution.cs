using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common;

[Table("Institution", Schema = "dbo")]
public class Institution: AuditModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long InstitutionIdId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string Address { get; set; }
    public required string Email { get; set; }
    public required string MobileNumber { get; set; }
}
