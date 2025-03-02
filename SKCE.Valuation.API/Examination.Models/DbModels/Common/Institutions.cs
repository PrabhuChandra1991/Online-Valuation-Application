using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common;

[Table("Institutions", Schema = "dbo")]
public class Institutions: AuditModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string MobileNumber { get; set; }
}
