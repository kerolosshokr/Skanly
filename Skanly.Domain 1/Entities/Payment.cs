using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;
namespace Skanly.Domain.Entities
{
    public  class Payment : BaseEntity
    {
        public Guid BookingId { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public DateTime PaymentDate { get; set; }

        public Booking Booking { get; set; } = null!;
    }
}
