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
    [Table("QPTemplateTagByUserDetails", Schema = "dbo")]
    public class QPTemplateTagByUserDetails:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long QPTemplateTagId { get; set; }
        public long QPTemplateId { get; set; }
        public required string QPTemplateTagValue { get; set; }
        public long UserId { get; set; }
    }
}
