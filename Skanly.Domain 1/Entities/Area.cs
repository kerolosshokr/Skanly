using Skanly.Domain.Entities.Common;
namespace Skanly.Domain.Entities
{
     public class Area : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;


        // Navigation Properties
        public ICollection<University> Universities { get; set; }
          = new List<University>();
        public ICollection<Property> Properties { get; set; } = new List<Property>();

    }
}
