using Skanly.Domain.Enums;
using Skanly.Domain.Entities.Common;
namespace Skanly.Domain.Entities;

public  class Booking : BaseEntity
{
    public Guid StudentId { get; set; }

    public Guid PropertyId { get; set; }

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public BookingStatus Status { get; set; }

    public Student Student { get; set; } = null!;

    public Property Property { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public Contract? Contract { get; set; }

    public Review? Review { get; set; }

}
