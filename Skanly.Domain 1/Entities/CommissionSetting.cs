// Skanly.Domain/Entities/CommissionSetting.cs
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class CommissionSetting : BaseEntity<int>, IAggregateRoot
{
    [Range(0, 100)]
    public decimal Rate { get; set; }

    [Required]
    public string SetByAdminId { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation
    public Admin SetByAdmin { get; set; } = null!;
}