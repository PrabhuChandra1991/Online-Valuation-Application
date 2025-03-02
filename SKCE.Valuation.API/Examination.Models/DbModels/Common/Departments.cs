
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("Departments", Schema ="dbo")]
    public class Departments: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public required string Name { get; set; }
       }
}
