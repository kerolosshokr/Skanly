using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Properties.DTOs;

public class PropertyDetailDto
{
    public int PropertyId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;
    public string? OwnerImageUrl { get; init; }
    public bool OwnerIsVerified { get; init; }

    public int? UniversityId { get; init; }
    public string? UniversityNameEn { get; init; }
    public string? UniversityNameAr { get; init; }

    public int AreaId { get; init; }
    public string AreaNameEn { get; init; } = string.Empty;
    public string AreaNameAr { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PropertyType PropertyType { get; init; }
    public string PropertyTypeDisplay => PropertyType.ToString();
    public GenderPolicy GenderPolicy { get; init; }
    public string GenderPolicyDisplay => GenderPolicy.ToString();
    public bool SmokingAllowed { get; init; }
    public int Rooms { get; init; }
    public int Beds { get; init; }
    public decimal PricePerMonth { get; init; }
    public string Address { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsApproved { get; init; }
    public decimal AverageRating { get; init; }
    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<PropertyImageDto> Images { get; init; }
        = new List<PropertyImageDto>();
    public IReadOnlyList<string> VideoUrls { get; init; }
        = new List<string>();
    public IReadOnlyList<AmenityDto> Amenities { get; init; }
        = new List<AmenityDto>();
    public IReadOnlyList<PropertyReviewDto> Reviews { get; init; }
        = new List<PropertyReviewDto>();

    public bool IsFavorited { get; init; }
    public bool HasActiveBooking { get; init; }
}

public class PropertyImageDto
{
    public int ImageId { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}

public class AmenityDto
{
    public int AmenityId { get; init; }
    public string NameEn { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public string? IconClass { get; init; }
}

public class PropertyReviewDto
{
    public int ReviewId { get; init; }
    public string StudentFullName { get; init; } = string.Empty;
    public string? StudentImageUrl { get; init; }
    public byte OverallRating { get; init; }
    public byte CleanlinessRating { get; init; }
    public byte SafetyRating { get; init; }
    public byte InternetRating { get; init; }
    public byte LocationRating { get; init; }
    public byte QuietnessRating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
