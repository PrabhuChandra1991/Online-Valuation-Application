using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("Course", Schema = "dbo")]
    public class Course:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CourseId { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
    }
}
