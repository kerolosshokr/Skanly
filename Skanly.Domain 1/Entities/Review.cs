using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities
{
    public  class Review : BaseEntity
    {
        public Guid BookingId { get; set; }

        public Guid StudentId { get; set; }

        public Guid PropertyId { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public Booking Booking { get; set; } = null!;

        public Student Student { get; set; } = null!;

        public Property Property { get; set; } = null!;
    }
}
