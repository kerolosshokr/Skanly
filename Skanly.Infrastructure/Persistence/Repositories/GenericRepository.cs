using System;
using System.Collections.Generic;
using System.Text;

namespace Skanly.Infrastructure.Persistence.Repositories
{
    public  class GenericRepository<T> : IRepository<T> 
        where T : class
    {
        private readonly SkanlyDbContext _context;

        public GenericRepository(SkanlyDbContext context)
        {
            _context = context;
        }

        // Implement IRepository methods
    }
}
