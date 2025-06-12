using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public class AnswersheetQuestionAnswerDto
    {
        public required int QuestionNumber { get; set; } = 0;
        public required int QuestionNumberSubNum { get; set; } = 0;
        public required string QuestionNumberDisplay { get; set; } = string.Empty;
        public required string QuestionPartName { get; set; } = string.Empty;
        public required string QuestionGroupName { get; set; } = string.Empty;
        public required string QuestionDescription { get; set; } = string.Empty;
        public string? QuestionImage { get; set; } = null;
        public string? QuestionBT { get; set; } = string.Empty;
        public string? QuestionCO { get; set; } = string.Empty;
        public string? QuestionMark { get; set; } = string.Empty;
        public string? AnswerDescription { get; set; } = string.Empty;
        public string? AnswerImage { get; set; } = string.Empty;
    }

    public class AnswersheetQuestionAnswerImageDto
    {
        public required int QuestionNumber { get; set; } = 0;
        public required int QuestionNumberSubNum { get; set; } = 0;
        public string? QuestionImage { get; set; } = null;
        public string? AnswerImage { get; set; } = string.Empty;
    }

}
