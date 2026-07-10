using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;

namespace Skanly.Domain.Entities
{
    public  class Property : BaseEntity
    {
        public Guid OwnerId { get; set; }

        public Guid AreaId { get; set; }

        public Guid? UniversityId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public decimal PricePerMonth { get; set; }

        public int AvailableBeds { get; set; }

   public PropertyType PropertyType { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public bool IsApproved { get; set; }

        public bool IsAvailable { get; set; }

        // Navigation Properties
        public Owner Owner { get; set; } = null!;

        public Area Area { get; set; } = null!;

        public University? University { get; set; }
        public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<ChatConversation> ChatConversations { get; set; }
    = new List<ChatConversation>();

    }
}
