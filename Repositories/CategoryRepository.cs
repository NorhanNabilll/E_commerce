
using Ecommerce.Data;
using ECommerce.Models;
using ECommerce.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommerce.Repositories
{
    public class CategoryRepository : Repositery<Category>, ICategoryRepository
    {
        private readonly AppDbContext _db;

        public CategoryRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Category>> GetAllWithProductsAsync()
        {
            return await _db.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new Category
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsDeleted = c.IsDeleted,
                })
                .ToListAsync();
        }

        /*
         public async Task<Category?> GetCategoryWithProductsAsync(Expression<Func<Category, bool>> filter)
         {
             var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
             return await _db.Categories
                 .Include(c => c.Products.Where(p => p.ApprovalStatus == "approved"))
                 .Select(c => new Category
                 {
                     Id = c.Id,
                     Name = c.Name,
                     IsDeleted = c.IsDeleted,
                     Products = c.Products.Select(p => new Product
                     {
                         Id = p.Id,
                         Name = p.Name,
                         Price = p.Price,
                         ImageUrl = p.ImageUrl,
                         CreatedAt = p.CreatedAt,
                         ApprovalStatus = p.ApprovalStatus
                     }).ToList()
                 })
                 .FirstOrDefaultAsync(filter);
         }*/

        public async Task<Category?> GetCategoryWithProductsAsync(Expression<Func<Category, bool>> filter)
        {
            return await _db.Categories
                .Where(filter)
                .Include(c => c.Products)
                .FirstOrDefaultAsync();
        }


        public async Task<bool> SoftDeleteAsync(Category category)
        {
            if (category == null) return false;
            
            category.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            if (category == null) return false;
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}