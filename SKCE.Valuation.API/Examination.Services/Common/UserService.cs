using SKCE.Examination.Services.ServiceContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SKCE.Examination.Models.DbModels.Common;
using Microsoft.Extensions.Configuration;

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
            .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(long id)
        {
            return await _context.Users
            .Include(u => u.UserCourses)
            .Include(u => u.UserAreaOfSpecializations)
            .FirstOrDefaultAsync(u => u.UserId == id); ;
        }

        public async Task<User> AddUserAsync(User user)
        {
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            _emailService.SendEmailAsync(user.Email, "Profile Update for Question Paper Setter Role – Sri Krishna Institutions, Coimbatore",
              $"Dear {user.Name}," +
              $"\n\nGreetings from Sri Krishna Institutions, Coimbatore! " +
              $"\n\nFurther to our previous communication, we are pleased to share the link {_configuration["LoginUrl"]} and Login Credentials to the form for updating your profile on our online platform. Kindly ensure that you complete this at your earliest convenience. You will also receive a notification via email regarding the appointment for the role of Question Paper Setter." +
              $"\n\nPlease note that the provided link allows only a one-time entry using your registered email address or username. If you encounter any issues or require any changes after submission, please feel free to contact us, and we will be happy to assist you." +
              $"\n\nThank you for your cooperation. We look forward to your valuable contribution to our institution \n\nWarm regards,\n[Your Name]\n[Your Position]\nSri Krishna Institutions, Coimbatore").Wait();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
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
    }
}
