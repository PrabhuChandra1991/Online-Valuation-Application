﻿using Microsoft.EntityFrameworkCore.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SKCE.Examination.Models.DbModels.Common
{
    [Table("Answersheet", Schema = "dbo")]
    public class Answersheet : AuditModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswersheetId { get; set; }
        public required long InstitutionId { get; set; }
        public required long CourseId { get; set; }
        public required string BatchYear { get; set; }
        public required string RegulationYear { get; set; }
        public required int Semester { get; set; }
        public required int DegreeTypeId { get; set; }
        public required string ExamMonth { get; set; }
        public required string ExamYear { get; set; }
        public required string DummyNumber { get; set; }
        public string? UploadedBlobStorageUrl { get; set; }


        public string? ScriptIdentity { get; set; }
        public long? AllocatedToUserId { get; set; }
        public DateTime? AllocatedDateTime { get; set; }
        public bool? IsAllocationMailSent { get; set; }
        public bool? IsEvaluateCompleted { get; set; }
        public long? EvaluatedByUserId { get; set; }
        public DateTime? EvaluatedDateTime { get; set; }

    }
}
