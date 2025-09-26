using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;
using ECommerce.Repositories.Interfaces;
using Ecommerce.Data;


namespace ECommerce.Repositories
{
    public class Repositery<T> : IRepositery<T> where T : class
    {
        private readonly AppDbContext db;
        internal DbSet<T> dbset;

        public Repositery(AppDbContext db)
        {
            this.db = db;
            this.dbset = db.Set<T>();
        }
        public async Task CreateAsync(T entity)
        {
            await dbset.AddAsync(entity);
            await db.SaveChangesAsync();

        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null)
        {
            IQueryable<T> query = dbset;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.ToListAsync();
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = dbset;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.FirstOrDefaultAsync();
        }

        public void  Delete(T entity)
        {
            dbset.Remove(entity);
            db.SaveChanges();
        }
    }


}
