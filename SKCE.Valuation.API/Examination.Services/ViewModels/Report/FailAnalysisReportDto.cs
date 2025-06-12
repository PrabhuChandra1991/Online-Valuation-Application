namespace SKCE.Examination.Services.ViewModels.Report
{
    public class FailAnalysisReportDto
    {
        public required long InstitutionId { get; set; }
        public required string InstitutionCode { get; set; }
        public required long CourseId { get; set; }
        public required string CourseCode { get; set; }
        public required string CourseName { get; set; }
        public required long StudentTotalRegisteredCount { get; set; }
        public required long StudentTotalAppearedCount { get; set; }
        public long StudentTotalAbsentCount
        {
            get
            {
                return this.StudentTotalRegisteredCount - StudentTotalAppearedCount;
            }
        }
        public required long StudentTotal_00_24_Count { get; set; }
        public required long StudentTotal_25_29_Count { get; set; }
        public required long StudentTotal_30_34_Count { get; set; }
        public required long StudentTotal_35_39_Count { get; set; }
        public required long StudentTotal_40_44_Count { get; set; }
        public long StudentTotalFailCount
        {
            get
            {
                return
                this.StudentTotal_00_24_Count +
                this.StudentTotal_25_29_Count +
                this.StudentTotal_30_34_Count +
                this.StudentTotal_35_39_Count +
                this.StudentTotal_40_44_Count;
            }
        }
    }
}
