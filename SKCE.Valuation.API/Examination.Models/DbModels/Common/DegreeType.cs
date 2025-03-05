
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("DegreeType", Schema = "dbo")]
    public class DegreeType:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DegreeTypeId { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
    }
}
