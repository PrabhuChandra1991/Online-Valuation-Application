namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetAllocateInputModel
    {
        public required long ExaminationId { get; set; } 
        public required long UserId { get; set; } 
        public required int Noofsheets { get; set; }

    }
}
