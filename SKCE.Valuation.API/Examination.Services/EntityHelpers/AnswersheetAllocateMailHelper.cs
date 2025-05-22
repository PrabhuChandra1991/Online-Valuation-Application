using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;

namespace SKCE.Examination.Services.EntityHelpers
{
    public class AnswersheetAllocateMailHelper
    {
        private readonly ExaminationDbContext _dbContext;
        public string  AppBaseUrl  { get; set; }
        private readonly EmailService _emailService;

        public AnswersheetAllocateMailHelper(ExaminationDbContext context, EmailService emailService, string appBaseUrl)
        {
            _dbContext = context;
            _emailService = emailService;
            AppBaseUrl = appBaseUrl;
        }

        public async Task SendAllocatedEmail(List<long> answersheetIds,  string emailId  )
        {

            var mailData =
                await (from ans in this._dbContext.Answersheets
                       join exam in this._dbContext.Examinations on ans.ExaminationId equals exam.ExaminationId
                       join user in this._dbContext.Users on ans.AllocatedToUserId equals user.UserId
                       join course in this._dbContext.Courses on exam.CourseId equals course.CourseId
                       where answersheetIds.Contains(ans.AnswersheetId)
                       select new
                       {
                           userName = user.Name,
                           exam.ExamMonth,
                           exam.ExamYear,
                           CourseCode = course.Code,
                           CourseName = course.Name
                       }).FirstAsync();

            var emailBody = Constants.allocationMail;

            emailBody = emailBody.Replace ("{recipient}", mailData.userName);
            emailBody = emailBody.Replace("{examMonth}", mailData.ExamMonth);
            emailBody = emailBody.Replace("{examYear}", mailData.ExamYear);
            emailBody = emailBody.Replace("{courseCode}", mailData.CourseCode);
            emailBody = emailBody.Replace("{courseName}", mailData.CourseName);
            emailBody = emailBody.Replace("{count}", answersheetIds.Count.ToString());
            emailBody = emailBody.Replace("{AppLoginUrl}",  this.AppBaseUrl );

            await _emailService.SendEmailHtml(emailId, "CONFIDENTIAL – Appointment - Valuation– Sri Krishna Institutions", emailBody);

        }



    } // Class
}
