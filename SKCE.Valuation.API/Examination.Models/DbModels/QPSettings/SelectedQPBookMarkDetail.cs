using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("SelectedQPBookMarkDetail", Schema = "dbo")]
    public class SelectedQPBookMarkDetail:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SelectedQPBookMarkDetailId { get; set; }
        public long SelectedQPDetailId { get; set; }
        public string BookMarkName { get; set; }
        public string? BookMarkText { get; set; }
    }
}
