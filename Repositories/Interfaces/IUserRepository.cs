using System.IdentityModel.Tokens.Jwt;
using ECommerce.DTOs.Authentication;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;


namespace ECommerce.Repositories.Interfaces
{
    public interface IUserRepository
    {      
        Task<bool> CheckPasswordAsync(AppUser user, string password);
        Task<string> CreateJwtToken(AppUser user);

        // Create
        Task<IdentityResult> CreateUserAsync(AppUser user, string password);
        Task<IdentityResult> AddUserToRoleAsync(AppUser user, string role);

        // Get
        Task<AppUser> GetUserByEmailAsync(string email);
        Task<String> GetRoleAsync(AppUser user);
        Task<AppUser?> GetUserByIdAsync(string userId);
        Task<List<UserDTO>> GetAllUsersAsync();

        // Delete
        Task<bool> DeleteUserAsync(string userId);
        


    }
}
