using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Skanly.Domain.Enums;

namespace Skanly.Application.Features.Properties.DTOs;

public class CreatePropertyDto
{
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

    [Display(Name = "Number of Rooms")]
    public int Rooms { get; set; } = 1;

    [Display(Name = "Number of Beds")]
    public int Beds { get; set; } = 1;

    [Display(Name = "Price Per Month (EGP)")]
    public decimal PricePerMonth { get; set; }

    [Display(Name = "Full Address")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Latitude")]
    public decimal Latitude { get; set; }

    [Display(Name = "Longitude")]
    public decimal Longitude { get; set; }

    [Display(Name = "Nearest University")]
    public int? UniversityId { get; set; }

    [Display(Name = "Area")]
    public int AreaId { get; set; }

    [Display(Name = "Amenities")]
    public List<int> AmenityIds { get; set; } = new();

    [Display(Name = "Property Photos")]
    public List<IFormFile> Images { get; set; } = new();

    [Display(Name = "Property Videos")]
    public List<IFormFile> Videos { get; set; } = new();

    public int PrimaryImageIndex { get; set; }
}
