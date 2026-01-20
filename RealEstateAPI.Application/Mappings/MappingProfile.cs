using AutoMapper;
using RealEstateAPI.Application.DTOs.Auth;
using RealEstateAPI.Application.DTOs.Property;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserDto = RealEstateAPI.Application.DTOs.Auth.UserDto;

namespace RealEstateAPI.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ========== USER MAPPINGS ==========

            // User → UserDto
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.Role,
                    opt => opt.MapFrom(src => src.Role.ToString()));

            // RegisterDto → User
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Ayrı set edilecek
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRole.User))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

            // ========== PROPERTY MAPPINGS ==========

            // Property → PropertyDto
            CreateMap<Property, PropertyDto>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.FullLocation,
                    opt => opt.MapFrom(src => $"{src.Neighborhood}, {src.District}, {src.City}"))
                .ForMember(dest => dest.User,
                    opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.Images,
                    opt => opt.MapFrom(src => src.PropertyImages.OrderBy(i => i.DisplayOrder)));

            // PropertyCreateDto → Property
            CreateMap<PropertyCreateDto, Property>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Token'dan alınacak
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.IsFeatured, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsPublished, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.PropertyImages, opt => opt.Ignore())
                .ForMember(dest => dest.Favorites, opt => opt.Ignore())
                .ForMember(dest => dest.Payments, opt => opt.Ignore())
                .ForMember(dest => dest.Contacts, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

            // PropertyUpdateDto → Property (Conditional mapping)
            CreateMap<PropertyUpdateDto, Property>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // ========== PROPERTY IMAGE MAPPINGS ==========

            // PropertyImage → PropertyImageDto
            CreateMap<PropertyImage, PropertyImageDto>();

            // PropertyImageDto → PropertyImage
            CreateMap<PropertyImageDto, PropertyImage>()
                .ForMember(dest => dest.PropertyId, opt => opt.Ignore())
                .ForMember(dest => dest.Property, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
        }
    }
}
