using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities
{
    public  class Favorite : BaseEntity
    {
        public Guid StudentId { get; set; }

        public Guid PropertyId { get; set; }

        public Student Student { get; set; } = null!;

        public Property Property { get; set; } = null!;
    }
}
