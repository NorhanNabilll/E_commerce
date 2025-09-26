using System.Linq.Expressions;

namespace ECommerce.Repositories.Interfaces
{
    public interface IRepositery<T> where T : class
    {
        Task CreateAsync(T entity);
        void Delete(T entity);
        Task<T> GetAsync(Expression<Func<T, bool>>? filter = null);
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null);
    }
}
