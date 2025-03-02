using Examination.Models.DbModels.Common;
using Examination.Models.DBModels.Common;
using Examination.Services.ServiceContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examination.Services.Common
{
    public class UserService : IUserService
    {
        private readonly ExaminationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        public UserService(ExaminationDbContext context,EmailService emailService,IConfiguration configuration)
        {
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
          _emailService.SendEmailAsync(user.Email, "Welcome to our SKCE Online Examination platform",
              $"Welcome {user.Email} to our SKCE Online Examination platform and please click (UI Host login Url) here to login for updating all profile details to proceed further.").Wait();
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
