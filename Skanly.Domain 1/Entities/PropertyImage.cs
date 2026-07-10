// Skanly.Domain/Entities/PropertyImage.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities;

public class PropertyImage : BaseEntity<int>
{
    [Required]
    public int PropertyId { get; set; }

    [Required, MaxLength(300)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Property Property { get; set; } = null!;
}