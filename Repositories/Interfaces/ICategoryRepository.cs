using ECommerce.Models;
using System.Linq.Expressions;

namespace ECommerce.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepositery<Category>
    {
        Task<Category> GetCategoryWithProductsAsync(Expression<Func<Category, bool>> filter);
        Task<bool> SoftDeleteAsync(Category category);
        Task<IEnumerable<Category>> GetAllWithProductsAsync();
        Task<bool> UpdateAsync(Category category);
    }
}