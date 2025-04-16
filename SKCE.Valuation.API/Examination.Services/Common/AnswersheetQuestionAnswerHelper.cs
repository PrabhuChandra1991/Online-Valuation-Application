using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.Common
{
    public class AnswersheetQuestionAnswerHelper
    {
        private readonly ExaminationDbContext _dbContext;

        public AnswersheetQuestionAnswerHelper(ExaminationDbContext context)
        {
            _dbContext = context;
        }


        public async Task<List<AnswersheetQuestionAnswerDto>> GetQuestionAndAnswersByAnswersheetId(long answersheetId)
        {
            var resultItems = new List<AnswersheetQuestionAnswerDto>();

            var answersheet = await this._dbContext.Answersheets
                .FirstOrDefaultAsync(x => x.AnswersheetId == answersheetId && x.IsActive);

            if (answersheet == null)
                return resultItems;

            var selectedQP = await this._dbContext.SelectedQPDetails
                .Where(x =>
                x.InstitutionId == answersheet.InstitutionId
                && x.CourseId == answersheet.CourseId
                && x.RegulationYear == answersheet.RegulationYear
                && x.BatchYear == answersheet.BatchYear
                && x.DegreeTypeId == answersheet.DegreeTypeId
                && x.ExamType == answersheet.ExamType
                && x.Semester == answersheet.Semester
                && x.ExamMonth == answersheet.ExamMonth
                && x.ExamYear == answersheet.ExamYear
                && x.IsActive).FirstAsync();

            if (selectedQP == null)
                return resultItems;

            var selectedQPMarks = await this._dbContext.SelectedQPBookMarkDetails
                .Where(x => x.SelectedQPDetailId == selectedQP.SelectedQPDetailId && x.IsActive).ToListAsync();

            if (selectedQPMarks.Count != 0)
            {
                int questionNumber = 0;
                while (questionNumber < 50)
                {
                    questionNumber++;

                    string qnNoStr = questionNumber.ToString();

                    string bkQuestionNo = "Q" + qnNoStr;
                    string bkQuestionNo1 = "Q" + qnNoStr + "I";
                    string bkQuestionNo2 = "Q" + qnNoStr + "II";

                    var newItem = GetQuestionAnswerItem(selectedQPMarks, bkQuestionNo, qnNoStr);
                    if (newItem!= null)
                    {
                        resultItems.Add(newItem);
                    }

                    var newItem1 = GetQuestionAnswerItem(selectedQPMarks, bkQuestionNo1, qnNoStr +"(i)");
                    if (newItem1 != null)
                    {
                        resultItems.Add(newItem1);
                    }

                    var newItem2 = GetQuestionAnswerItem(selectedQPMarks, bkQuestionNo2, qnNoStr + "(ii)");
                    if (newItem1 != null)
                        if (newItem2 != null)
                    {
                        resultItems.Add(newItem2);
                    } 

                }//While 
            }//If


            return resultItems;





        }


        private static AnswersheetQuestionAnswerDto? GetQuestionAnswerItem(List<SelectedQPBookMarkDetail> selectedQPMarks,
            string bkQuestionNo, string questionNoDisplay)
        {
            string bkQuestionIMG = bkQuestionNo + "IMG";
            string bkQuestionBT = bkQuestionNo + "BT";
            string bkQuestionCO = bkQuestionNo + "CO";
            string bkQuestionMARKS = bkQuestionNo + "MARKS";
            string bkQuestionAK = bkQuestionNo + "AK";

            var QPBookMark = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionNo);

            if (QPBookMark != null)
            {
                var QPBookMarkImg = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionIMG);
                var QPBookMarkBT = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionBT);
                var QPBookMarkCO = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionCO);
                var QPBookMarkMark = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionMARKS);
                var AnswerBookMark = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionAK);

                var newItem = new AnswersheetQuestionAnswerDto
                {
                    QuestionNumber = bkQuestionNo,
                    QuestionNumberDisplay = questionNoDisplay,
                    QuestionDescription = QPBookMark.BookMarkText ?? string.Empty,
                    QuestionImage = QPBookMarkImg?.BookMarkText ?? string.Empty,
                    QuestionBT = QPBookMarkBT?.BookMarkText ?? string.Empty,
                    QuestionCO = QPBookMarkCO?.BookMarkText ?? string.Empty,
                    QuestionMark = QPBookMarkMark?.BookMarkText ?? string.Empty,
                    AnswerDescription = AnswerBookMark?.BookMarkText ?? string.Empty,
                };
                return newItem;
            }
            return null;
        }


    } // Class
}
