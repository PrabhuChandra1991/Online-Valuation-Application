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
        public required string RegulationYear { get; set; }
        public required string BatchYear { get; set; }
        public  long DegreeTypeId { get; set; }
        public long DepartmentId { get; set; }
        public long ExamMonthId { get; set; }
        public long? Semester { get; set; }
        public long? StudentCount { get; set; }
    }
}
