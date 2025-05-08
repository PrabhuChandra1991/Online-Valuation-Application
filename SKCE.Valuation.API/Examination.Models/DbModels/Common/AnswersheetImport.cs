using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("AnswersheetImport", Schema = "dbo")]
    public class AnswersheetImport : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetImportId { get; set; }
        public required string DocumentName { get; set; } = string.Empty;
        public required string DocumentUrl { get; set; } = string.Empty;
        public required long InstitutionId { get; set; } = 0;
        public required string ExamMonth { get; set; }
        public required string ExamYear { get; set; }
        public required long CourseId { get; set; } = 0;
        public bool? IsReviewCompleted { get; set; } = null;
        public  DateTime? ReviewCompletedOn { get; set; } = null;
        public  long? ReviewCompletedBy { get; set; } = null;

        public virtual ICollection<AnswersheetImportDetail> AnswersheetImportDetails { get; set; } = [];
    }
}
