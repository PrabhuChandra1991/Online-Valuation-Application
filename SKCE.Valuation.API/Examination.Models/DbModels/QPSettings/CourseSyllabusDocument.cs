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
    [Table("CourseSyllabusDocument", Schema = "dbo")]
    public class CourseSyllabusDocument:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CourseSyllabusDocumentId { get; set; }
        public long CourseId { get; set; }
        public long DocumentId { get; set; }
        public long? WordDocumentId { get; set; }
    }
}
