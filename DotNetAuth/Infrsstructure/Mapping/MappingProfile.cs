using AutoMapper;
using DotNetAuth.Domain.Constracts;
using DotNetAuth.Domain.Entities;

namespace DotNetAuth.Infrsstructure.Mapping
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, UserResponse>();
            CreateMap<ApplicationUser, CurrentUserResponse>();
            CreateMap<UserRegisterRequest,ApplicationUser>();
        }
    }
}
