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
                  $"\n\nWe are pleased to share the link below for updating your profile on our online platform: {_configuration["LoginUrl"]}"+
                  $"\n\nKindly complete the update at your earliest convenience. Upon completion and acceptance by you, a separate email will be sent regarding your appointment as a Question Paper Setter/Evaluator." +
                  $"\n\nIf you require any assistance, please feel free to contact us." +
                  $"\n\nWarm regards,\nDr. Ramesh Kumar R,\nDean – Examinations,\nSri Krishna Institutions,\n8300034477.").Wait();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.IsActive = true;
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
        public async Task<bool> CheckSameNameOtherUserExists(User user, long userId)
        {
            return  _context.Users.Any(u => (u.Email == user.Email || u.MobileNumber == user.MobileNumber) && u.UserId != userId);
        }
    }
}
