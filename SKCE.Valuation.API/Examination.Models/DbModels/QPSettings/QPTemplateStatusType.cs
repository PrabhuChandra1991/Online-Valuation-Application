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
        [Table("QPTemplateStatusType", Schema = "dbo")]
        public class QPTemplateStatusType : AuditModel
        {

            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public long QPTemplateStatusTypeId { get; set; }
            public required string Name { get; set; }
        }
}
