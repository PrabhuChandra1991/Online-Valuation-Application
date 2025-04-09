using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("QPTemplateTag", Schema = "dbo")]
    public class QPTemplateTag: AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPTemplateTagId { get; set; }
        public long QPTemplateId { get; set; }
        public long QPTagId { get; set; }
        public string? QPTagValue { get; set; }
    }
}
