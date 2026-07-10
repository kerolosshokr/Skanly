using Skanly.Domain.Entities.Common;
using Skanly.Domain.Enums;

namespace Skanly.Domain.Entities
{
    public  class Report : BaseEntity
    {
        public string ReporterId { get; set; } = string.Empty;

        public Guid PropertyId { get; set; }

        public ReportType ReportType { get; set; }

        public string Reason { get; set; } = string.Empty;

        public bool IsResolved { get; set; } = false;

        public string? ResolvedByAdminId { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public Property Property { get; set; } = null!;
    }
}
