using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("ImportHistory", Schema = "dbo")]
    public class ImportHistory:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ImportHistoryId { get; set; }
        public long DocumentId { get; set; }
        public long UserId { get; set; }
        public long TotalCount { get; set; }
        public long CoursesCount { get; set; }
        public long InstitutionsCount { get; set; }
        public long DepartmentsCount { get; set; }
    }
}
