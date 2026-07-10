using Skanly.Domain.Entities.Common;
using Skanly.Domain_1.Enums;
namespace Skanly.Domain.Entities
{
     public class Owner : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        public string? ProfileImage { get; set; }

        public string? NationalIdImage { get; set; }
        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}
