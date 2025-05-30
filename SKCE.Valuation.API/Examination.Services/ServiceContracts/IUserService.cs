﻿using SKCE.Examination.Models.DbModels.Common;

namespace SKCE.Examination.Services.ServiceContracts
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<User?> GetUserOnlyByIdAsync(long id);
        Task<User> GetUserByIdAsync(long id);
        Task<User> AddUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(long id);
        Task<bool> CheckSameNameOtherUserExists(User user, long userId);
    }
}
