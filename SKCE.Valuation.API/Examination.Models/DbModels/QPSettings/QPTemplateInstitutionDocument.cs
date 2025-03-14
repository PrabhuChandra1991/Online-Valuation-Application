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
    [Table("QPTemplateInstitutionDocument", Schema = "dbo")]
    public class QPTemplateInstitutionDocument:AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QPTemplateInstitutionDocumentId { get; set; }
        [ForeignKey("QPTemplateInstitution")]
        public long QPTemplateInstitutionId { get; set; }
        public long QPDocumentTypeId { get; set; }
        public long DocumentId { get; set; }
    }
}
