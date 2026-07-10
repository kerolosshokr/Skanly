// Skanly.Infrastructure/Persistence/Configurations/Seed/SeedDataConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skanly.Domain.Entities;

namespace Skanly.Infrastructure.Persistence.Configurations.Seed;

// Applied via builder.ApplyConfiguration(new SeedDataConfiguration()) is not how
// HasData works per-entity, so seeding is instead added inside each entity's own
// configuration class below for clarity. Shown here consolidated for readability.
public static class SeedDataConfiguration
{
    public static void SeedUniversities(this EntityTypeBuilder<University> builder)
    {
        builder.HasData(
            new University { Id = 1, NameAr = "جامعة القاهرة", NameEn = "Cairo University", Latitude = 30.0289m, Longitude = 31.2154m, IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new University { Id = 2, NameAr = "جامعة عين شمس", NameEn = "Ain Shams University", Latitude = 30.0722m, Longitude = 31.2853m, IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new University { Id = 3, NameAr = "الجامعة الأمريكية بالقاهرة", NameEn = "American University in Cairo", Latitude = 30.0192m, Longitude = 31.4995m, IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new University { Id = 4, NameAr = "جامعة حلوان", NameEn = "Helwan University", Latitude = 29.8556m, Longitude = 31.3350m, IsActive = true, CreatedAt = new DateTime(2025, 1, 1) }
        );
    }

    public static void SeedAreas(this EntityTypeBuilder<Area> builder)
    {
        builder.HasData(
            new Area { Id = 1, NameAr = "المعادي", NameEn = "Maadi", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 2, NameAr = "مدينة نصر", NameEn = "Nasr City", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 3, NameAr = "القاهرة الجديدة", NameEn = "New Cairo", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 4, NameAr = "التجمع الخامس", NameEn = "Fifth Settlement", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 5, NameAr = "الدقي", NameEn = "Dokki", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 6, NameAr = "المهندسين", NameEn = "Mohandessin", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 7, NameAr = "حلوان", NameEn = "Helwan", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 8, NameAr = "6 أكتوبر", NameEn = "6th October", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 9, NameAr = "الشيخ زايد", NameEn = "Sheikh Zayed", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) },
            new Area { Id = 10, NameAr = "الشروق", NameEn = "Shorouk", IsActive = true, CreatedAt = new DateTime(2025, 1, 1) }
        );
    }

    public static void SeedAmenities(this EntityTypeBuilder<Amenity> builder)
    {
        builder.HasData(
            new Amenity { Id = 1, NameAr = "واي فاي", NameEn = "WiFi", IconClass = "fa-wifi" },
            new Amenity { Id = 2, NameAr = "تكييف", NameEn = "Air Conditioner", IconClass = "fa-snowflake" },
            new Amenity { Id = 3, NameAr = "ثلاجة", NameEn = "Refrigerator", IconClass = "fa-refrigerator" },
            new Amenity { Id = 4, NameAr = "غسالة", NameEn = "Washing Machine", IconClass = "fa-soap" },
            new Amenity { Id = 5, NameAr = "مطبخ", NameEn = "Kitchen", IconClass = "fa-kitchen-set" },
            new Amenity { Id = 6, NameAr = "ميكروويف", NameEn = "Microwave", IconClass = "fa-microwave" },
            new Amenity { Id = 7, NameAr = "سخان مياه", NameEn = "Water Heater", IconClass = "fa-fire" },
            new Amenity { Id = 8, NameAr = "أسانسير", NameEn = "Elevator", IconClass = "fa-elevator" },
            new Amenity { Id = 9, NameAr = "كاميرات مراقبة", NameEn = "Security Cameras", IconClass = "fa-video" },
            new Amenity { Id = 10, NameAr = "حارس أمن", NameEn = "Security Guard", IconClass = "fa-shield" },
            new Amenity { Id = 11, NameAr = "موقف سيارات", NameEn = "Parking", IconClass = "fa-square-parking" },
            new Amenity { Id = 12, NameAr = "كهرباء شاملة", NameEn = "Electricity Included", IconClass = "fa-bolt" },
            new Amenity { Id = 13, NameAr = "مياه شاملة", NameEn = "Water Included", IconClass = "fa-droplet" }
        );
    }
}