using Skanly.Domain.Entities.Common;
namespace Skanly.Domain.Entities
{
     public class Owner : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public bool IsVerified { get; set; }

    }
}
