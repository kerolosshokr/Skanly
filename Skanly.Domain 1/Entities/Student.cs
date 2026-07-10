using Skanly.Domain.Entities.Common;
using Skanly.Domain_1.Enums;
using System;

namespace Skanly.Domain.Entities
{
    public  class Student : BaseEntity
    {
        // Identity User
        public string UserId { get; set; } = string.Empty;

        // Student Data
        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public Gender Gender { get; set; }

        public string? ProfileImage { get; set; }

        // University
        public Guid UniversityId { get; set; }

        // Navigation Property
        public University University { get; set; } = null!;
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<IdentityVerification> IdentityVerifications { get; set; }
    = new List<IdentityVerification>();

    }
}
