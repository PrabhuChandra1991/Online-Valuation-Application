using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("UserAreaOfSpecialization", Schema = "dbo")]
    public class UserAreaOfSpecialization:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserAreaOfSpecializationId { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }

        public required string AreaOfSpecializationName { get; set; }

        public required virtual User User { get; set; }

    }
}
