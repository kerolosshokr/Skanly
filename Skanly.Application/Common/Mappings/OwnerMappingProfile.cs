using AutoMapper;
using Skanly.Application.Features.Owners.DTOs;
using Skanly.Domain.Entities;

namespace Skanly.Application.Common.Mappings;

public class OwnerMappingProfile : Profile
{
    public OwnerMappingProfile()
    {
        CreateMap<Owner, OwnerProfileDto>()
            .ForMember(d => d.Email,             opt => opt.Ignore())
            .ForMember(d => d.PhoneNumber,        opt => opt.Ignore())
            .ForMember(d => d.VerificationStatus, opt => opt.Ignore())
            .ForMember(d => d.TotalProperties,    opt => opt.Ignore())
            .ForMember(d => d.ActiveListings,     opt => opt.Ignore())
            .ForMember(d => d.TotalBookings,      opt => opt.Ignore())
            .ForMember(d => d.PendingRequests,    opt => opt.Ignore())
            .ForMember(d => d.TotalEarnings,      opt => opt.Ignore())
            .ForMember(d => d.UserId,
                opt => opt.MapFrom(s => s.UserId));

        CreateMap<UpdateOwnerProfileDto, Owner>()
            .ForMember(d => d.UserId,       opt => opt.Ignore())
            .ForMember(d => d.Properties,   opt => opt.Ignore())
            .ForMember(d => d.Conversations,opt => opt.Ignore())
            .ForMember(d => d.CreatedAt,    opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt,    opt => opt.Ignore());
    }
}
