using System;
using System.Collections.Generic;
using System.Text;

namespace Skanly.Infrastructure.Persistence.Repositories
{
    public  class GenericRepository<T> : IRepository<T> 
        where T : class
    {
        private readonly ApplicationDbContext _context;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Implement IRepository methods
    }
}
