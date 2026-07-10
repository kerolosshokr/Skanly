using Skanly.Domain.Entities.Common;
namespace Skanly.Domain.Entities
{
    public  class PropertyImage : BaseEntity
    {
        public Guid PropertyId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public Property Property { get; set; } = null!;
    }
}

