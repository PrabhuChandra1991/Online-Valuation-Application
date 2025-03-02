using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("CourseDetails", Schema = "dbo")]
    public class CourseDetails:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CourseId { get; set; }
        public required string CourseName { get; set; }
        public required string CourseCode { get; set; }
        public required string CourseDuration { get; set; }
        public required string Semester { get; set; }
    }
}
