using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("AnswersheetImportDetail", Schema = "dbo")]
    public class AnswersheetImportDetail : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetImportDetailId { get; set; }
        public long AnswersheetImportId { get; set; }
        public required string InstitutionCode { get; set; } = string.Empty;
        public required string RegulationYear { get; set; } = string.Empty;
        public required string BatchYear { get; set; } = string.Empty;
        public required string DegreeType { get; set; } = string.Empty;
        public required string ExamType { get; set; } = string.Empty;
        public required string CourseCode { get; set; } = string.Empty; 
        public required string ExamMonth { get; set; }
        public required string ExamYear { get; set; } 
        public required string DummyNumber { get; set; }
    }
}
