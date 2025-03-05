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
    [Table("UserQPTemplateTag", Schema = "dbo")]
    public class UserQPTemplateTag: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserQPTemplateTagId { get; set; }
        public long UserQPTemplateId { get; set; }
        public long QPTagId { get; set; }
        public string? QPTagValue { get; set; }
    }
}
