using SKCE.Examination.Models.DbModels.Common;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SKCE.Examination.Services.Common
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

        public async Task<string?> GenerateTempPasswordAsync(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return null;

            string tempPassword = new Random().Next(100000, 999999).ToString();
            _tempPasswords[email] = (tempPassword, DateTime.UtcNow.AddMinutes(5));

            var loginHistory = new UserLoginHistory
            {
                UserId = user.UserId,
                Email = email,
                TempPassword = tempPassword, // Store hashed password
                LoginDateTime = DateTime.UtcNow,
                IsSuccessful = false
            };
            AuditHelper.SetAuditPropertiesForInsert(loginHistory, user.UserId);
            await _context.UserLoginHistories.AddAsync(loginHistory);
            await _context.SaveChangesAsync();
            _emailService.SendEmailAsync(email, "SKCE Online Examination Platform: Your Temporary Password", $"Hi {user.Name},\n\n Please use your Email Id and Temporary Password to login into SKCE Online Examination Platform. \n\n Your temporary password is: {tempPassword} (valid for 20 minutes) \n\n Thanks\n SKCE Admin").Wait();

            return tempPassword;
        }

        public async Task<User?> ValidateTempPassword(string email, string password)
        {
            var loginRecord = await _context.UserLoginHistories
            .Where(l => l.Email == email)
            .OrderByDescending(l => l.LoginDateTime)
            .FirstOrDefaultAsync();

            if (loginRecord != null && _tempPasswords.TryGetValue(email, out var data) &&
                (data.tempPassword == password && data.expiry >= DateTime.UtcNow))
            {
                loginRecord.IsSuccessful = true;
                AuditHelper.SetAuditPropertiesForUpdate(loginRecord, loginRecord.UserId);
                _context.UserLoginHistories.Update(loginRecord);
                await _context.SaveChangesAsync();

                _tempPasswords.TryRemove(email, out _);
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                return user;
            }
            return null;
        }

        /// <summary>
        /// Hash password using SHA256.
        /// </summary>
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Verify hashed password.
        /// </summary>
        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            return HashPassword(inputPassword) == hashedPassword;
        }

    }
}
