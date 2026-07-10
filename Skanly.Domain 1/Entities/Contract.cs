
using Skanly.Domain.Entities.Common;
namespace Skanly.Domain.Entities
{
    public  class Contract : BaseEntity
    {
        public Guid BookingId { get; set; }

        public string FileUrl { get; set; } = string.Empty;

        public DateTime SignedAt { get; set; }

        public Booking Booking { get; set; } = null!;
    }
}
