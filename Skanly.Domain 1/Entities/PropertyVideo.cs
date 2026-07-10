using Skanly.Domain.Entities.Common;
namespace Skanly.Domain.Entities
{
    public  class PropertyVideo : BaseEntity
    {
        public Guid PropertyId { get; set; }

        public string VideoUrl { get; set; } = string.Empty;

        public Property Property { get; set; } = null!;
    }
}
