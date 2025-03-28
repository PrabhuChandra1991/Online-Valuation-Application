﻿using SKCE.Examination.Models.DbModels.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Models.DbModels.QPSettings
{
    [Table("UserQPTemplate", Schema = "dbo")]
    public class UserQPTemplate : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserQPTemplateId { get; set; }
        public long UserId { get; set; }
        [ForeignKey("QPTemplateInstitution")]
        public long QPTemplateInstitutionId { get; set; }
        public long QPTemplateStatusTypeId { get; set; }
        [ForeignKey("QPDocument")]
        public long QPDocumentId { get; set; }
        public bool IsQPOnly { get; set; }
        public virtual ICollection<UserQPTemplateDocument> Documents { get; set; } = new List<UserQPTemplateDocument>();

    }
}
