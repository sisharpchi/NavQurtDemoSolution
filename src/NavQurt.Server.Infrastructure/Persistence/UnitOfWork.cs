using Microsoft.EntityFrameworkCore;
using NavQurt.Server.Core.Persistence;

namespace NavQurt.Server.Infrastructure.Persistence
{
    internal class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
    {
        private readonly TContext _context;

        public UnitOfWork(TContext context)
        {
            _context = context;
        }

        public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
