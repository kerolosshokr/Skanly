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
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<PropertyVideo> PropertyVideos { get; set; }
        public DbSet<PropertyAmenity> PropertyAmenities { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();




        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
