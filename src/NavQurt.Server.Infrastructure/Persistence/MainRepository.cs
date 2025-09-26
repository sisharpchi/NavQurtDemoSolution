using NavQurt.Server.Core.Persistence;
using NavQurt.Server.Infrastructure.Data;

namespace NavQurt.Server.Infrastructure.Persistence
{
    internal class MainRepository : GenericRepository<MainDbContext>, IMainRepository
    {
        public MainRepository(
            MainDbContext context,
            UnitOfWork<MainDbContext> unitOfWork) : base(context, unitOfWork)
        {
        }
    }
}
