using AutoMapper;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Helpers
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, BaseUserDto>();
            CreateMap<User, UserForAdminsDto>();
        }
    }
}
