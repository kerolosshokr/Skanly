using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities
{
    public class University : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string? Address { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public bool IsActive { get; set; } = true;
    }
}