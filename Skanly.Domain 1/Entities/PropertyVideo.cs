// Skanly.Domain/Entities/PropertyVideo.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities;

public class PropertyVideo : BaseEntity<int>
{
    [Required]
    public int PropertyId { get; set; }

    [Required, MaxLength(300)]
    public string VideoUrl { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Property Property { get; set; } = null!;
}