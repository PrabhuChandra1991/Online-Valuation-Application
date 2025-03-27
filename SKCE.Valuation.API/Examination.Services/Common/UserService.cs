using SKCE.Examination.Services.ServiceContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using Microsoft.Extensions.Configuration;
using SKCE.Examination.Models.DbModels.QPSettings;

namespace SKCE.Examination.Services.Common
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ExaminationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        public UserService(IHttpContextAccessor  httpContextAccessor, ExaminationDbContext context, EmailService emailService, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await  _context.Users
            .Include(u => u.UserCourses)
            .Include(u => u.UserAreaOfSpecializations)
            .Include(u => u.UserQualifications)
            .Include(u => u.UserDesignations)
            .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(long id)
        {
            return await _context.Users
            .Include(u => u.UserCourses)
            .Include(u => u.UserAreaOfSpecializations)
            .Include(u => u.UserQualifications)
            .Include(u => u.UserDesignations)
            .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User> AddUserAsync(User user)
        {
            try
            {
                user.IsEnabled = false;
                //seed default qualifications
                foreach (var degreeType in _context.DegreeTypes.OrderBy(d=>d.DegreeTypeId))
                {
                    var degreeTypeName = string.Format("{0}{1}:",degreeType.DegreeTypeId == 2?"*":"", degreeType.Name);
                    var userQualification = new UserQualification { UserId = user.UserId, Title = degreeTypeName, Specialization = "", Name = "", IsCompleted = false };
                    AuditHelper.SetAuditPropertiesForInsert(userQualification, 1);
                    user.UserQualifications.Add(userQualification);
                }
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                //send email
                _emailService.SendEmailAsync(user.Email, "Profile Update - Question Paper Setter/Evaluator – Sri Krishna Institutions, Coimbatore",
                  $"Dear {user.Name}," +
                  $"\n\nGreetings from Sri Krishna Institutions, Coimbatore!" +
                  $"\n\nFurther to our previous communication, we are pleased to share with you the link {_configuration["LoginUrl"]} for updating your profile on our online platform. We request you to complete the update at your earliest convenience. Additionally, you will receive a separate notification via email regarding your appointment for the role of Question Paper Setter/Evaluator."+
                  $"\n\nIf you require any further clarification, please do not hesitate to contact us. We will be happy to assist you." +
                  $"\n\nContact Details:\nName:\nContact Number:\n\nThank you for your cooperation. We look forward to your valuable contribution to our institution.\n\nWarm regards,\nSri Krishna College of Engineering and Technology").Wait();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            AuditHelper.SetAuditPropertiesForUpdate(user, 1);
            foreach (var userCourse in user.UserCourses)
            {
                AuditHelper.SetAuditPropertiesForUpdate(userCourse, 1);
            }
            foreach (var userAreaOfSpecialization in user.UserAreaOfSpecializations)
            {
                AuditHelper.SetAuditPropertiesForUpdate(userAreaOfSpecialization, 1);
            }
            foreach (var userDesignation in user.UserDesignations)
            {
                AuditHelper.SetAuditPropertiesForUpdate(userDesignation, 1);
            }
            foreach (var userQualification in user.UserQualifications)
            {
                AuditHelper.SetAuditPropertiesForUpdate(userQualification, 1);
            }
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> CheckSameNameOtherUserExists(string Name, long userId)
        {
            return  _context.Users.Any(u => u.Name == Name && u.UserId != userId);
        }
    }
}
