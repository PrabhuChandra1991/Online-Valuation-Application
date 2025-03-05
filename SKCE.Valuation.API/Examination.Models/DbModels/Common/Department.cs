
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("Department", Schema ="dbo")]
    public class Department: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DepartmentId { get; set; }
        public required string Name { get; set; }

        public required string ShortName { get; set; }

        public long DegreeTypeId { get; set; }
    }
}
