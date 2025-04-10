
using SKCE.Examination.Models.DbModels.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("SelectedQPDetail", Schema = "dbo")]
    public class SelectedQPDetail:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SelectedQPDetailId { get; set; }
        public long InstitutionId { get; set; }
        public long CourseId { get; set; }
        public string RegulationYear { get; set; }
        public string BatchYear { get; set; }
        public long DegreeTypeId { get; set; }
        public string ExamType { get; set; }
        public long Semester { get; set; }
        public string ExamMonth { get; set; }
        public string ExamYear { get; set; }
        public long QPPrintedDocumentId { get; set; }
        public long QPPrintedWordDocumentId { get; set; }
        public DateTime QPPrintedDate { get; set; }
        public long QPPrintedById { get; set; }
        
    }
}
