using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.EntityHelpers
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
                    && x.Semester == examination.Semester
                    && x.ExamMonth == examination.ExamMonth
                    && x.ExamYear == examination.ExamYear
                    && x.IsActive);

            if (selectedQP == null)
                return resultItems;

            var selectedQPMarks = await _dbContext.SelectedQPBookMarkDetails
                .Where(x => x.SelectedQPDetailId == selectedQP.SelectedQPDetailId && x.IsActive).ToListAsync();

            var degreeType = await _dbContext.DegreeTypes.FirstAsync(x => x.DegreeTypeId == selectedQP.DegreeTypeId);

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
                    if (newItem1 != null)
                        if (newItem2 != null)
                        {
                            resultItems.Add(newItem2);
                        }

                }//While 
            }//If


            return resultItems;





        }


        private static AnswersheetQuestionAnswerDto? GetQuestionAnswerItem(
            List<SelectedQPBookMarkDetail> selectedQPMarks, string bkQuestionNo,
            int questionNumber, int questionNumberSubNum, string degreeType)
        {
            string bkQuestionIMG = bkQuestionNo + "IMG";
            string bkQuestionBT = bkQuestionNo + "BT";
            string bkQuestionCO = bkQuestionNo + "CO";
            string bkQuestionMARKS = bkQuestionNo + "MARKS";
            string bkQuestionAK = bkQuestionNo + "AK";
            string bkAnswerIMG = bkQuestionNo + "AKIMG";

            var QPBookMark = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionNo);

            if (QPBookMark != null)
            {
                var QPBookMarkImg = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionIMG);
                var QPBookMarkBT = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionBT);
                var QPBookMarkCO = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionCO);
                var QPBookMarkMark = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionMARKS);
                var AnswerBookMark = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkQuestionAK);
                var AnswerImg = selectedQPMarks.FirstOrDefault(x => x.BookMarkName == bkAnswerIMG);

                var questionDisp = GetQuestionNumberDisplay(questionNumber, questionNumberSubNum);
                var questionPart = GetQuestionPart(questionNumber, degreeType);
                var questionGroup = GetQuestionGroupName(questionNumber, questionPart);

                var newItem = new AnswersheetQuestionAnswerDto
                {
                    QuestionNumber = questionNumber,
                    QuestionNumberSubNum = questionNumberSubNum,
                    QuestionNumberDisplay = questionDisp,
                    QuestionPartName = questionPart,
                    QuestionGroupName = questionGroup,
                    QuestionDescription = QPBookMark.BookMarkText ?? string.Empty,
                    QuestionImage = QPBookMarkImg?.BookMarkText ?? string.Empty,
                    QuestionBT = QPBookMarkBT?.BookMarkText ?? string.Empty,
                    QuestionCO = QPBookMarkCO?.BookMarkText ?? string.Empty,
                    QuestionMark = QPBookMarkMark?.BookMarkText ?? string.Empty,
                    AnswerDescription = AnswerBookMark?.BookMarkText ?? string.Empty,
                    AnswerImage = AnswerImg?.BookMarkText ?? string.Empty
                };
                return newItem;
            }
            return null;
        }


        private static string GetQuestionNumberDisplay(int Num, int num2)
        {
            if (num2 == 1)
                return Num.ToString() + "(i)";
            else if (num2 == 2)
                return Num.ToString() + "(ii)";
            else
                return Num.ToString();
        }

        private static string GetQuestionPart(long qnNo, string degreeType)
        {
            string partVal = "";
            if (degreeType == "UG")
            {
                if (qnNo > 10)
                    partVal = "B";
                else
                    partVal = "A";
            }
            else if (degreeType == "PG")
            {
                if (qnNo > 18)
                    partVal = "C";
                else if (qnNo > 10)
                    partVal = "B";
                else
                    partVal = "A";
            }
            return partVal;
        }

        private static string GetQuestionGroupName(long questionNum, string questionPart)
        {
            string groupName = "";

            if (questionNum >= 1 && questionNum <= 10)
                groupName = questionPart + questionNum.ToString();
            else if (questionNum == 11 || questionNum == 12)
                groupName = questionPart + "11OR12";
            else if (questionNum == 13 || questionNum == 14)
                groupName = questionPart + "13OR14";
            else if (questionNum == 15 || questionNum == 16)
                groupName = questionPart + "15OR16";
            else if (questionNum == 17 || questionNum == 18)
                groupName = questionPart + "17OR18";
            else if (questionNum == 19 || questionNum == 20)
                groupName = questionPart + "19OR20";

            return groupName;
        }


    } // Class
}
