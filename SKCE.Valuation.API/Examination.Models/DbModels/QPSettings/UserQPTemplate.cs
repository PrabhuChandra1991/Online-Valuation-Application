using SKCE.Examination.Models.DbModels.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("UserQPTemplate", Schema = "dbo")]
    public class UserQPTemplate : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserQPTemplateId { get; set; }
        public long UserId { get; set; }
        public long QPTemplateStatusTypeId { get; set; }
        [ForeignKey("QPDocument")]
        public long QPDocumentId { get; set; }
        public bool IsQPOnly { get; set; }
        public long InstitutionId { get; set; }
        public long QPTemplateId { get; set; }
        public long SubmittedQPDocumentId { get; set; }
        public long? ParentUserQPTemplateId { get; set; }
        public bool? IsGraphsRequired { get; set; }
        public bool? IsTablesAllowed { get; set; }
        public string? GraphName { get; set; }
        public string? TableName { get; set; }
        public string? QPCode { get; set; }
        public bool? IsQPSelected { get; set; }
        public long? UserQPDocumentId { get; set; }
    }
}
