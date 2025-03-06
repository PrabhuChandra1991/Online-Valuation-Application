using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("UserCourse", Schema = "dbo")]
    public class UserCourse : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserCourseId { get; set; }

        [ForeignKey("User")]
        public long UserId { get; set; }

        public required string CourseName { get; set; }

        public long NumberOfYearsHandled { get; set; }

        public bool IsHandledInLast2Semester { get; set; }

        public required virtual User User { get; set; }

    }
}
