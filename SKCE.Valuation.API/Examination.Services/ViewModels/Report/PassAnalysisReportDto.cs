﻿namespace SKCE.Examination.Services.ViewModels.Report
{
    public class PassAnalysisReportDto
    {
        public required long InstitutionId { get; set; }
        public required string InstitutionCode { get; set; }
        public required long CourseId { get; set; }
        public required string CourseCode { get; set; }
        public required string CourseName { get; set; }
        public required long StudentTotalRegisteredCount { get; set; }
        public required long StudentTotalAppearedCount { get; set; }
        public required long StudentTotalAbsentCount { get; set; }
        public required long StudentTotal_45_50_Count { get; set; }
        public required long StudentTotal_51_60_Count { get; set; }
        public required long StudentTotal_61_70_Count { get; set; }
        public required long StudentTotal_71_80_Count { get; set; }
        public required long StudentTotal_81_90_Count { get; set; }
        public required long StudentTotal_91_100_Count { get; set; }
        public required long StudentTotalPassCount { get; set; } 
    }
}
