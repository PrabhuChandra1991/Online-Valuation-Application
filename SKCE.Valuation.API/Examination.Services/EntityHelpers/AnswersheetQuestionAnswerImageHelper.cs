using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.Services.EntityHelpers
{
    public class AnswersheetQuestionAnswerImageHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public AnswersheetQuestionAnswerImageHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }


        public async Task<List<AnswersheetQuestionAnswerImageDto>>
           GetQuestionAndAnswerImagesByAnswersheetId(long answersheetId, int questionNumber, int questionSubNumber)
        {

            var resultItems = new List<AnswersheetQuestionAnswerImageDto>();

            var answersheet = await _dbContext.Answersheets.FirstOrDefaultAsync(x => x.AnswersheetId == answersheetId);

            if (answersheet == null)
                return resultItems;

            var examination =
                await this._dbContext.Examinations
                .FirstOrDefaultAsync(x => x.ExaminationId == answersheet.ExaminationId);

            if (examination == null)
                return resultItems;

            var selectedQP = await _dbContext.SelectedQPDetails
                    .FirstOrDefaultAsync(x =>
                    x.CourseId == examination.CourseId
                    && x.RegulationYear == examination.RegulationYear
                    && x.BatchYear == examination.BatchYear
                    && x.DegreeTypeId == examination.DegreeTypeId
                    && x.ExamType == examination.ExamType
                    //&& x.Semester == examination.Semester
                    && x.ExamMonth == examination.ExamMonth
                    && x.ExamYear == examination.ExamYear
                    && x.IsActive);

            if (selectedQP == null)
                return resultItems;

            var selectedQPMarks = await _dbContext.SelectedQPBookMarkDetails
                .Where(x => x.SelectedQPDetailId == selectedQP.SelectedQPDetailId
                && x.BookMarkName.Contains("Q" + questionNumber.ToString())
                && x.IsActive).ToListAsync();

            var degreeType = await _dbContext.DegreeTypes.FirstAsync(x => x.DegreeTypeId == selectedQP.DegreeTypeId);

            if (selectedQPMarks.Count != 0)
            {
                string qnNoStr = questionNumber.ToString();
                string bkQuestionNo = "Q" + qnNoStr;
                string bkQuestionNo1 = "Q" + qnNoStr + "I";
                string bkQuestionNo2 = "Q" + qnNoStr + "II";

                var newItem = GetQuestionAnswerItem(selectedQPMarks, bkQuestionNo, questionNumber, 0, degreeType.Code);
                if (newItem != null)
                {
                    resultItems.Add(newItem);
                }

                var newItem1 = GetQuestionAnswerItem(selectedQPMarks, bkQuestionNo1, questionNumber, 1, degreeType.Code);
                if (newItem1 != null)
                {
                    resultItems.Add(newItem1);
                }

                var newItem2 = GetQuestionAnswerItem(selectedQPMarks, bkQuestionNo2, questionNumber, 2, degreeType.Code);
                if (newItem2 != null)
                {
                    resultItems.Add(newItem2);
                }

            }//If

            return resultItems.Where(x => x.QuestionNumber == questionNumber && x.QuestionNumberSubNum == questionSubNumber).ToList();
        
        }


        private static AnswersheetQuestionAnswerImageDto? GetQuestionAnswerItem(
            List<SelectedQPBookMarkDetail> selectedQPMarks, string bkQuestionNo,
            int questionNumber, int questionNumberSubNum, string degreeType)
        {
            string bkQuestionIMG = bkQuestionNo + "IMG";
            string bkAnswerIMG = bkQuestionNo + "AKIMG";
            var QPBookMarkImg = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionIMG);
            var AnswerImg = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkAnswerIMG);
            if (QPBookMarkImg != null || AnswerImg != null)
            {
                var newItem = new AnswersheetQuestionAnswerImageDto
                {
                    QuestionNumber = questionNumber,
                    QuestionNumberSubNum = questionNumberSubNum,
                    QuestionImage = QPBookMarkImg?.BookMarkText ?? string.Empty,
                    AnswerImage = AnswerImg?.BookMarkText ?? string.Empty
                };
                return newItem;
            }
            return null;
        }

    } // Class
}
