// Skanly.Application/Common/Mappings/UniversityMappingProfile.cs
using AutoMapper;
using Skanly.Application.Features.Universities.DTOs;
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Mappings;

public class UniversityMappingProfile : Profile
{
    public UniversityMappingProfile()
    {
        // Entity → DTO
        CreateMap<University, UniversityDto>()
            .ForMember(dest => dest.UniversityId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TotalProperties, opt => opt.Ignore())
            .ForMember(dest => dest.TotalStudents, opt => opt.Ignore());

        // CreateDto → Entity
        CreateMap<CreateUniversityDto, University>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Students, opt => opt.Ignore())
            .ForMember(dest => dest.Properties, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // UpdateDto → Entity (for _mapper.Map(dto, entity) pattern)
        CreateMap<UpdateUniversityDto, University>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Students, opt => opt.Ignore())
            .ForMember(dest => dest.Properties, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}