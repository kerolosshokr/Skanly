using AutoMapper;
using Skanly.Application.Features.Properties.DTOs;
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Mappings;

public class PropertyMappingProfile : Profile
{
    public PropertyMappingProfile()
    {
        CreateMap<Property, PropertyCardDto>()
            .ForMember(d => d.PropertyId,         opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.AreaNameEn,          opt => opt.MapFrom(s => s.Area.NameEn))
            .ForMember(d => d.UniversityNameEn,    opt => opt.MapFrom(s => s.University!.NameEn))
            .ForMember(d => d.PropertyTypeDisplay, opt => opt.MapFrom(s => s.PropertyType.ToString()))
            .ForMember(d => d.PrimaryImageUrl,     opt => opt.MapFrom(s =>
                s.Images.FirstOrDefault(i => i.IsPrimary)!.ImageUrl))
            .ForMember(d => d.IsFavorited,         opt => opt.Ignore());
    }
}
