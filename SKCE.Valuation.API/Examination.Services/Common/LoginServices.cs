using Examination.Models.DbModels.Common;
using Examination.Models.DBModels.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Examination.Services.Common
{
    public class LoginServices
    {
        private readonly ExaminationDbContext _context;
        private readonly EmailService _emailService;
        private static ConcurrentDictionary<string, (string tempPassword, DateTime expiry)> _tempPasswords = new();

        public LoginServices(ExaminationDbContext context,EmailService emailService)
        {
            _emailService = emailService;
            _context = context;
        }

        public async Task<string> GenerateTempPasswordAsync(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return null;

            string tempPassword = new Random().Next(100000, 999999).ToString();
            _tempPasswords[email] = (tempPassword, DateTime.UtcNow.AddMinutes(5));

            _emailService.SendEmailAsync(email, "Your Temporary Password", $"Your temporary password is: {tempPassword} (valid for 5 minutes)").Wait();
            //await SendEmailAsync(user.Email, tempPassword);

            return tempPassword;
        }

        public User ValidateTempPassword(string email, string password)
        {
            if (_tempPasswords.TryGetValue(email, out var data) &&
                data.tempPassword == password && data.expiry > DateTime.UtcNow)
            {
                _tempPasswords.TryRemove(email, out _);
                return null;
            }
            return (_context.Users.FirstOrDefault(u => u.Email == email));
        }

    }
}
