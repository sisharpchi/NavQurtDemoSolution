using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NavQurt.Server.Core.Persistence;
using NavQurt.Server.Infrastructure.Data;
using System.Linq.Expressions;

namespace NavQurt.Server.Infrastructure.Persistence
{
    internal class CompanyRepository : GenericRepository<AppDbContext>, ICompanyRepository
    {
        private readonly AppDbContext _dbcontext;

        public CompanyRepository(
            AppDbContext context,
            UnitOfWork<AppDbContext> unitOfWork) : base(context, unitOfWork)
        {
            _dbcontext = context;
        }
        public AppDbContext DbContext => _dbcontext;
        //public DatabaseFacade Database => _dbcontext.Database;

        //public string CurrentCompanyId => _dbcontext.CurrentCompanyToken!;
        //public Organization CurrentOrganization => _dbcontext.CurrentOrganization!;

        async Task<TEntity?> ICompanyRepository.GetAsync<TEntity>(object? id)
            where TEntity : class
        {
            var entity = await Context.Set<TEntity>().FindAsync(id);
            return entity;
        }

        Task<TEntity?> ICompanyRepository.GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            return Context.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }

        Task<List<TEntity>> ICompanyRepository.GetListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate)
             where TEntity : class
        {
            return predicate == default ? Context.Set<TEntity>().ToListAsync()
                : Context.Set<TEntity>().Where(predicate).ToListAsync();
        }

        Task<Dictionary<TKey, TEntity>> ICompanyRepository.GetDictionaryAsync<TKey, TEntity>(
            Func<TEntity, TKey> keySelector,
            Expression<Func<TEntity, bool>>? predicate)
            where TEntity : class
        {
            return predicate == default ? Context.Set<TEntity>().ToDictionaryAsync(keySelector)
                : Context.Set<TEntity>().Where(predicate).ToDictionaryAsync(keySelector);
        }

        IQueryable<TEntity> ICompanyRepository.Query<TEntity>(Expression<Func<TEntity, bool>>? predicate)
             where TEntity : class
        {
            return predicate == default
                ? Context.Set<TEntity>()
                : Context.Set<TEntity>().Where(predicate);
        }

        async Task ICompanyRepository.AddAsync<TEntity>(TEntity entity)
            where TEntity : class
        {
            await Context.Set<TEntity>().AddAsync(entity);
        }

        void ICompanyRepository.Update<TEntity>(TEntity entity)
            where TEntity : class
        {
            Context.Set<TEntity>().Attach(entity);
            Context.Entry(entity).State = EntityState.Modified;
        }

        void ICompanyRepository.Delete<TEntity>(TEntity? entity)
            where TEntity : class
        {
            if (entity == null)
                return;

            Context.Set<TEntity>().Remove(entity);
        }

        public IExecutionStrategy CreateExecutionStrategy()
        {
            return _dbcontext.Database.CreateExecutionStrategy();
        }
        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return _dbcontext.Database.BeginTransactionAsync();
        }

        public Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities)
            where TEntity : class
        {
            return Context.Set<TEntity>().AddRangeAsync(entities);
        }

        DbSet<TEntity> ICompanyRepository.Set<TEntity>()
        {
            return Context.Set<TEntity>();
        }

        void ICompanyRepository.DeleteRange<TEntity>(IEnumerable<TEntity> entities)
        {
            Context.Set<TEntity>().RemoveRange(entities);
        }
    }
}
