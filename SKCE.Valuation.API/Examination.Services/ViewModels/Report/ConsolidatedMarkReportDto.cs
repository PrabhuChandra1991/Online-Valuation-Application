namespace SKCE.Examination.Services.ViewModels.Report
{
    public class ConsolidatedMarkReportDto
    {
        public required long InstitutionId { get; set; } = 0;
        public required string InstitutionCode { get; set; } = string.Empty;
        public required long CourseId { get; set; } = 0;
        public required string CourseCode { get; set; } = string.Empty;
        public required string CourseName { get; set; } = string.Empty;
        public required long StudentTotalRegisteredCount { get; set; } = 0;
        public required long StudentTotalAppearedCount { get; set; } = 0;
        public long StudentTotalAbsentCount { get { return StudentTotalRegisteredCount - StudentTotalAppearedCount; } }
        public required long StudentTotalPassCount { get; set; } = 0;
        public required long StudentTotalFailCount { get; set; } = 0;
    }
}
