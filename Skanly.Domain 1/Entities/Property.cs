// Skanly.Domain/Entities/Property.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Property : BaseEntity<int>, IAggregateRoot
{
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public int? UniversityId { get; set; }

    [Required]
    public int AreaId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PropertyType PropertyType { get; set; }

    public bool SmokingAllowed { get; set; }

    [Range(1, int.MaxValue)]
    public int Rooms { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int Beds { get; set; } = 1;

    [Range(0.01, double.MaxValue)]
    public decimal PricePerMonth { get; set; }

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    public bool IsAvailable { get; set; } = true;
    public bool IsApproved { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    [Range(0, 5)]
    public decimal AverageRating { get; set; } = 0;

    // Navigation
    public Owner Owner { get; set; } = null!;
    public University? University { get; set; }
    public Area Area { get; set; } = null!;
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<PropertyVideo> Videos { get; set; } = new List<PropertyVideo>();
    public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}