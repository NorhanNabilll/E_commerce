using Ecommerce.Data;
using ECommerce.DTOs;
using ECommerce.Models;
using Google;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Services
{
    // Services/PointsService.cs
    public class PointsService : IPointsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PointsService> _logger;
        private readonly IConfiguration _configuration;

        public PointsService(
            AppDbContext context,
            ILogger<PointsService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<UserPoints> GetUserPointsAsync(string userId)
        {
            try
            {
                var userPoints = await _context.UserPoints
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userPoints == null)
                {
                    // Create initial points record for new user
                    userPoints = new UserPoints
                    {
                        UserId = userId,
                        TotalPoints = 0,
                        AvailablePoints = 0,
                        LastUpdated = DateTime.UtcNow
                    };

                    _context.UserPoints.Add(userPoints);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created initial points record for user {UserId}", userId);
                }

                return userPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user points for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AddPointsAsync(string userId, int points, string description, int? orderId = null)
        {
            if (points <= 0)
            {
                _logger.LogWarning("Attempted to add invalid points amount: {Points} for user: {UserId}", points, userId);
                return false;
            }

            // Check if we're already in a transaction
            var shouldManageTransaction = _context.Database.CurrentTransaction == null;
            var transaction = shouldManageTransaction ? await _context.Database.BeginTransactionAsync() : null;

            try
            {
                // Get or create user points record
                var userPoints = await GetUserPointsAsync(userId);

                // Update points
                userPoints.TotalPoints += points;
                userPoints.AvailablePoints += points;
                userPoints.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                var pointTransaction = new PointTransaction
                {
                    UserId = userId,
                    Points = points,
                    Type = PointTransactionType.Earned,
                    Description = description,
                    OrderId = orderId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.PointTransactions.Add(pointTransaction);
                await _context.SaveChangesAsync();

                if (shouldManageTransaction && transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation("Added {Points} points to user {UserId}. New balance: {Balance}",
                    points, userId, userPoints.AvailablePoints);

                return true;
            }
            catch (Exception ex)
            {
                if (shouldManageTransaction && transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                _logger.LogError(ex, "Error adding points for user {UserId}", userId);
                return false;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        public async Task<bool> UsePointsAsync(string userId, int points, int? orderId = null)
        {
            if (points <= 0)
            {
                _logger.LogWarning("Attempted to use invalid points amount: {Points} for user: {UserId}", points, userId);
                return false;
            }

            // Check if we're already in a transaction "this was for fixing transaction error"
            var shouldManageTransaction = _context.Database.CurrentTransaction == null;
            var transaction = shouldManageTransaction ? await _context.Database.BeginTransactionAsync() : null;

            try
            {
                var userPoints = await GetUserPointsAsync(userId);

                // Check if user has enough points
                if (userPoints.AvailablePoints < points)
                {
                    _logger.LogWarning("Insufficient points for user {UserId}. Requested: {RequestedPoints}, Available: {AvailablePoints}",
                        userId, points, userPoints.AvailablePoints);
                    return false;
                }

                // Update points
                userPoints.AvailablePoints -= points;
                userPoints.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                var pointTransaction = new PointTransaction
                {
                    UserId = userId,
                    Points = -points, // Negative for used points
                    Type = PointTransactionType.Used,
                    Description = orderId.HasValue ? $"Used for order #{orderId}" : "Points redeemed",
                    OrderId = orderId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.PointTransactions.Add(pointTransaction);
                await _context.SaveChangesAsync();

                if (shouldManageTransaction && transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation("Used {Points} points for user {UserId}. Remaining balance: {Balance}",
                    points, userId, userPoints.AvailablePoints);

                return true;
            }
            catch (Exception ex)
            {
                if (shouldManageTransaction && transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                _logger.LogError(ex, "Error using points for user {UserId}", userId);
                return false;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

    }

}
