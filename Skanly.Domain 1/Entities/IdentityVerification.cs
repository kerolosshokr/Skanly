using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;

namespace Skanly.Domain.Entities
{
    public class IdentityVerification : BaseEntity
    {
        public Guid StudentId { get; set; }

        public string DocumentUrl { get; set; } = string.Empty;

        public VerificationStatus Status { get; set; }

        public Guid? ReviewedByAdminId { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public Student Student { get; set; } = null!;

        public Admin? ReviewedByAdmin { get; set; }
    }
}
