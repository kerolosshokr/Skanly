using Skanly.Domain.Entities.Common;
using System;

namespace Skanly.Domain.Entities
{
    public  class Student : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public bool IsVerified { get; set; }

    }
}
