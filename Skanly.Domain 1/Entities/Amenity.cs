// Skanly.Domain/Entities/Amenity.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Amenity : BaseEntity<int>, IAggregateRoot
{
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string NameEn { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? IconClass { get; set; }

    // Navigation
    public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();
}