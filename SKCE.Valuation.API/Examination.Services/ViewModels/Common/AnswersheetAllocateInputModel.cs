namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetAllocateInputModel
    {
        public required string ExamYear { get; set; } 
        public required string ExamMonth { get; set; } 
        public required string ExamType { get; set; } 
        public required long CourseId { get; set; } 
        public required long UserId { get; set; } 
        public required int Noofsheets { get; set; }

    }
}
