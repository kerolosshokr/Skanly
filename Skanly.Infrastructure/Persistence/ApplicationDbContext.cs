using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        //DbSets
        public DbSet<Area> Areas { get; set; }
        public DbSet<University> Universities { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Owner> Owners { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
