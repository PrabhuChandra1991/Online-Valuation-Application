using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        public required long ExaminationId { get; set; } = 0;
        public virtual ICollection<AnswersheetImportDetail> AnswersheetImportDetails { get; set; } = [];
    }
}
