using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("Examination", Schema = "dbo")]
    public class Examination : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ExaminationId { get; set; }
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
        public bool IsQPPrinted { get; set; }
        public long? QPPrintedById { get; set; }
        public DateTime? QPPrintedDate { get; set; }        
        public virtual ICollection<Answersheet> Answersheets { get; set; } = new List<Answersheet>();
    }
}
