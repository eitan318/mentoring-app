using MentoringApp.Data.Interfaces;

namespace MentoringApp.Data.Access.SQLEF
{
    internal class EFDbRepo : IDbRepo
    {
        private readonly MentoringDbContext _context;

        public EFDbRepo(MentoringDbContext context)
        {
            _context = context;
        }

        public void Recreate() 
        {
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }
    }
}