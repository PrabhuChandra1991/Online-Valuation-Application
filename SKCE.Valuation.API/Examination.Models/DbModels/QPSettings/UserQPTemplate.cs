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
    [Table("UserQPTemplate", Schema = "dbo")]
    public class UserQPTemplate : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserQPTemplateId { get; set; }
        public long UserId { get; set; }
        public long QPTemplateId { get; set; }
        public long QPTemplateStatusTypeId { get; set; }
        public long UserQPDocumentId { get; set; }
        public long UserQPAnswerDocumentId { get; set; }
    }
}
