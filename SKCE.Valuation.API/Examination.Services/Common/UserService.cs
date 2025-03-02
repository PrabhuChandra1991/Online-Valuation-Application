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
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
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
            _emailService.SendEmailAsync(user.Email, "Welcome to SKCE Online Examination Platform",
              $"Welcome {user.Email} to our SKCE Online Examination Platform and please click {_configuration["LoginUrl"]} here to login for updating all your profile details to proceed further.\n\n Please contract SKCE Administrator if any challenges in login.\n\n Thanks\n SKCE Admin").Wait();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
