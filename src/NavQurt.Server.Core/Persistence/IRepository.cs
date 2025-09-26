using System.Collections.Generic;
using System.Linq.Expressions;

namespace NavQurt.Server.Core.Persistence
{
    public interface IRepository : IDisposable
    {
        /// <summary>
        /// Unit of work
        /// </summary>
        IUnitOfWork UnitOfWork { get; }

        /// <summary>
        /// Asynchronously counts number of entities.
        /// </summary>
        /// <param name="predicate">Predicate to filter query</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>Number of elements that satisfies the specified condition</returns>
        Task<int> CountAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class;

        /// <summary>
        /// Asynchronously computes the sum of a sequence of values
        /// </summary>
        /// <param name="selector">A projection function to apply to each element</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>The sum of the projected values</returns>
        Task<decimal> SumAsync<TEntity>(Expression<Func<TEntity, decimal>> selector, Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class;

        /// <summary>
        /// Gets an entity object by ID.
        /// </summary>
        /// <param name="id">Entity primary key</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>Entity object</returns>
        Task<TEntity?> GetAsync<TEntity>(object? id)
            where TEntity : class;

        /// <summary>
        /// Gets an entity object filtered by predicate.
        /// </summary>
        /// <param name="predicate">Predicate to filter query</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>Entity object</returns>
        Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class;

        /// <summary>
        /// Gets a list of the entity objects
        /// </summary>
        /// <param name="predicate">Predicate to filter query</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>List of entity objects</returns>
        Task<List<TEntity>> GetListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class;

        /// <summary>
        /// Gets a dictionary of the entities and specified keys.
        /// </summary>
        /// <param name="keySelector">Key selector</param>
        /// <param name="predicate">Predicate to filter query</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <typeparam name="TKey">Dictionary key type</typeparam>
        Task<Dictionary<TKey, TEntity>> GetDictionaryAsync<TKey, TEntity>(
            Func<TEntity, TKey> keySelector,
            Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class
            where TKey : notnull;

        /// <summary>
        /// Gets IQueryable entity objects.
        /// </summary>
        /// <param name="predicate">Predicate to filter query</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>IQueryable of entity objects</returns>
        IQueryable<TEntity> Query<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
            where TEntity : class;

        /// <summary>
        /// Adds new entry to database.
        /// </summary>
        /// <param name="entity">Entity object</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        Task AddAsync<TEntity>(TEntity entity)
            where TEntity : class;

        /// <summary>
        /// Updates existing entry.
        /// </summary>
        /// <param name="entity">Entity object</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        void Update<TEntity>(TEntity entity)
            where TEntity : class;

        /// <summary>
        /// Deletes entity object from database.
        /// </summary>
        /// <param name="entity">Entity object</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        void Delete<TEntity>(TEntity? entity)
            where TEntity : class;
    }
}
