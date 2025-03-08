using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("UserQualification",Schema ="dbo")]
    public class UserQualification:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserQualificationId { get; set; }
        [ForeignKey("User")]
        public long UserId { get; set; }
        public required string Title { get; set; }
        public required string Name { get; set; }
        public required string Specialization { get; set; }
        public bool? IsCompleted { get; set; }
    }
}
