// Skanly.Domain/Entities/Area.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Area : BaseEntity<int>, IAggregateRoot
{
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string NameEn { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Property> Properties { get; set; } = new List<Property>();
}