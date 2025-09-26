using Microsoft.EntityFrameworkCore;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Core.Persistence;
using System.Linq.Expressions;

namespace NavQurt.Server.Infrastructure.Persistence
{
    internal class GenericRepository<TContext> : IRepository
        where TContext : DbContext
    {
        protected readonly TContext Context;

        protected GenericRepository(
            TContext context,
            IUnitOfWork unitOfWork)
        {
            Context = context;
            UnitOfWork = unitOfWork;
        }

        public IUnitOfWork UnitOfWork { get; }

        public Task<int> CountAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class, IEntity<int>
        {
            return predicate == default ? Context.Set<TEntity>().CountAsync()
                : Context.Set<TEntity>().CountAsync(predicate);
        }

        public Task<decimal> SumAsync<TEntity>(Expression<Func<TEntity, decimal>> selector, Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class, IEntity<int>
        {

            return predicate == default ? Context.Set<TEntity>().SumAsync(selector) :
                Context.Set<TEntity>().Where(predicate).SumAsync(selector);
        }

        public async Task<TEntity?> GetAsync<TEntity>(object? id)
            where TEntity : class
        {
            var entity = await Context.Set<TEntity>().FindAsync(id);
            return entity;
        }

        public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class, IEntity<int>
        {
            return Context.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }

        public Task<List<TEntity>> GetListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class, IEntity<int>
        {
            return predicate == default ? Context.Set<TEntity>().ToListAsync()
                : Context.Set<TEntity>().Where(predicate).ToListAsync();
        }

        public Task<Dictionary<TKey, TEntity>> GetDictionaryAsync<TKey, TEntity>(
            Func<TEntity, TKey> keySelector,
            Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class, IEntity<int>
            where TKey : notnull
        {
            return predicate == default ? Context.Set<TEntity>().ToDictionaryAsync(keySelector)
                : Context.Set<TEntity>().Where(predicate).ToDictionaryAsync(keySelector);
        }

        public IQueryable<TEntity> Query<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class
        {
            return predicate == default
                ? Context.Set<TEntity>()
                : Context.Set<TEntity>().Where(predicate);
        }

        public async Task AddAsync<TEntity>(TEntity entity)
            where TEntity : class, IEntity<int>
        {
            await Context.Set<TEntity>().AddAsync(entity);
        }

        public void Update<TEntity>(TEntity entity)
            where TEntity : class, IEntity<int>
        {
            Context.Set<TEntity>().Attach(entity);
            Context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete<TEntity>(TEntity? entity)
            where TEntity : class, IEntity<int>
        {
            if (entity == null)
                return;

            Context.Set<TEntity>().Remove(entity);
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
