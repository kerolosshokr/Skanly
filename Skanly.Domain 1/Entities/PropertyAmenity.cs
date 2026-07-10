using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skanly.Domain.Entities
{

    // PropertyAmenity doesn't inherit from BaseEntity because it's only a junction table.
    public class PropertyAmenity
    {
        public Guid PropertyId { get; set; }

        public Guid AmenityId { get; set; }

        public Property Property { get; set; } = null!;

        public Amenity Amenity { get; set; } = null!;

    }
}
