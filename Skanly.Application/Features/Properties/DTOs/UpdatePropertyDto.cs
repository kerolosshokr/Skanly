using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Properties.DTOs;

public class UpdatePropertyDto
{
    public int PropertyId { get; set; }

    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Property Type")]
    public PropertyType PropertyType { get; set; }

    [Display(Name = "Gender Policy")]
    public GenderPolicy GenderPolicy { get; set; }

    [Display(Name = "Smoking Allowed")]
    public bool SmokingAllowed { get; set; }

    [Display(Name = "Rooms")]
    public int Rooms { get; set; }

    [Display(Name = "Beds")]
    public int Beds { get; set; }

    [Display(Name = "Price Per Month (EGP)")]
    public decimal PricePerMonth { get; set; }

    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Latitude")]
    public decimal Latitude { get; set; }

    [Display(Name = "Longitude")]
    public decimal Longitude { get; set; }

    [Display(Name = "University")]
    public int? UniversityId { get; set; }

    [Display(Name = "Area")]
    public int AreaId { get; set; }

    [Display(Name = "Available")]
    public bool IsAvailable { get; set; }

    [Display(Name = "Amenities")]
    public List<int> AmenityIds { get; set; } = new();

    [Display(Name = "Add Photos")]
    public List<IFormFile> NewImages { get; set; } = new();

    [Display(Name = "Add Videos")]
    public List<IFormFile> NewVideos { get; set; } = new();

    public List<int> DeleteImageIds { get; set; } = new();

    public int? PrimaryImageId { get; set; }
}
