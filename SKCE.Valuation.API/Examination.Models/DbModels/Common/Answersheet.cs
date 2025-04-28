using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("Answersheet", Schema = "dbo")]
    public class Answersheet : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetId { get; set; }

        [ForeignKey("Examination")] 
        public long ExaminationId { get; set; }     
        
        public required string DummyNumber { get; set; }
        public string? UploadedBlobStorageUrl { get; set; }

        public string? ScriptIdentity { get; set; }
        public long? AllocatedToUserId { get; set; }
        public DateTime? AllocatedDateTime { get; set; }
        public bool? IsAllocationMailSent { get; set; }
        
        public bool IsEvaluateCompleted { get; set; }
        public long? EvaluatedByUserId { get; set; }
        public DateTime? EvaluatedDateTime { get; set; }

        public decimal TotalObtainedMark { get; set; }
    }
}
