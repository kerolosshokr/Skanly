// Skanly.Domain/Entities/Favorite.cs
using Skanly.Domain.Entities.Common;
using Skanly.Domain.Interfaces;

namespace Skanly.Domain.Entities;

public class Favorite : BaseEntity<int>, IAggregateRoot
{
    public string StudentId { get; set; } = string.Empty;
    public int PropertyId { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
    public Property Property { get; set; } = null!;
}