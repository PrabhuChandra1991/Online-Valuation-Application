using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ViewModels.Common
{
    public  class AnswersheetQuestionAnswerDto
    {
        public required string QuestionNumber { get; set; } = string.Empty;
        public required string QuestionNumberDisplay { get; set; } = string.Empty;
        public required string  QuestionDescription { get; set; } = string.Empty;
        public string? QuestionImage { get; set; } = null;
        public string? QuestionBT { get; set; } = string.Empty;
        public string? QuestionCO { get; set; } = string.Empty;
        public required string  QuestionMark { get; set; } = string.Empty;
        public required string  AnswerDescription { get; set; } = string.Empty;

    }
}
