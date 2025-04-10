using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("AnswersheetImportHistory", Schema = "dbo")]
    public class AnswersheetImportHistory : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetImportHistoryId { get; set; }
        public required string Name { get; set; }
        public required string Url { get; set; } 
    }
}
