namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetImportDetailDto  
    {
        public long AnswersheetImportDetailId { get; set; }
        public long AnswersheetImportId { get; set; } = 0;
        public long ExaminationId { get; set; } = 0;
        public required string InstitutionCode { get; set; } = string.Empty;
        public required string RegulationYear { get; set; } = string.Empty;
        public required string BatchYear { get; set; } = string.Empty;
        public required string DegreeType { get; set; } = string.Empty;
        public required string DepartmentShortName { get; set; } = string.Empty;
        public required string ExamType { get; set; } = string.Empty;
        public required int Semester { get; set; } = 0;
        public required string CourseCode { get; set; } = string.Empty;
        public required string ExamMonth { get; set; } = string.Empty;
        public required string ExamYear { get; set; } = string.Empty;
        public required string DummyNumber { get; set; } = string.Empty;
        public required bool IsAnswerSheetUploaded { get; set; } = false;
        public required bool IsValid { get; set; } = false;
        public required string ErrorMessage { get; set; } = string.Empty; 
    }
}
