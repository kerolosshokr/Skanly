using Skanly.Domain.Entities.Common;
namespace Skanly.Domain.Entities
{
    public  class Amenity : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
