using Skanly.Domain.Entities.Common;

namespace Skanly.Domain.Entities
{
    public  class Admin : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
        public ICollection<IdentityVerification> ReviewedIdentityVerifications { get; set; }
    = new List<IdentityVerification>();
    }
}
