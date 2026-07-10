// Skanly.Domain/Entities/University.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class University : BaseEntity<int>, IAggregateRoot
{
    [Required, MaxLength(150)]
    public string NameAr { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string NameEn { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<Property> Properties { get; set; } = new List<Property>();
}