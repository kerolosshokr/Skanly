// Skanly.Domain/Entities/PropertyAmenity.cs
namespace Skanly.Domain.Entities;

// Pure join entity for the Property <-> Amenity many-to-many relationship.
// Composite PK (PropertyId, AmenityId) configured via Fluent API in Part 4.
public class PropertyAmenity
{
    public int PropertyId { get; set; }
    public int AmenityId { get; set; }

    // Navigation
    public Property Property { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}