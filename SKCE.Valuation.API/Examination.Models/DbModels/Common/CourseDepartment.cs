using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("CourseDepartment", Schema = "dbo")]
    public class CourseDepartment: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CourseDepartmentId { get; set; }
        public required long InstitutionId { get; set; }
        public required long CourseId { get; set; }
        public required long DepartmentId { get; set; }
        public required string RegulationYear { get; set; }
        public required string BatchYear { get; set; }
        public required long DegreeTypeId { get; set; }
        public required string ExamType { get; set; }
        public required long Semester { get; set; }
        public required string ExamMonth { get; set; }
        public required string ExamYear { get; set; }
        public required long StudentCount { get; set; }
    }
}
