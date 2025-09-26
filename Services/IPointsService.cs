using ECommerce.DTOs;
using ECommerce.Models;

namespace ECommerce.Services
{
    public interface IPointsService
    {
        Task<UserPoints> GetUserPointsAsync(string userId);
        Task<bool> AddPointsAsync(string userId, int points, string description, int? orderId = null);
        Task<bool> UsePointsAsync(string userId, int points, int? orderId = null);
   
    }
}
