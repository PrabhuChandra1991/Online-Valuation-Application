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
    [Table("QPTags", Schema = "dbo")]
    public class QPTags:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Tag { get; set; }
        public required string TagDataType { get; set; }
        public bool IsQuestion { get; set; }
        public required string Description { get; set; }
        
    }
}
