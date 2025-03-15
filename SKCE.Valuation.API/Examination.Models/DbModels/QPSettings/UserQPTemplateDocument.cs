using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("UserQPTemplateDocument", Schema = "dbo")]
    public class UserQPTemplateDocument:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserQPTemplateDocumentId { get; set; }
        [ForeignKey("UserQPTemplate")]
        public long UserQPTemplateId { get; set; }
        public long QPDocumentTypeId { get; set; }
        public long DocumentId { get; set; }
    }
}
