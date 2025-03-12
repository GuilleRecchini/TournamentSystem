using AutoMapper;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Helpers
{
    public class GameMappingProfile : Profile
    {
        public GameMappingProfile()
        {
            CreateMap<User, BaseUserDto>();
            CreateMap<Game, GameDto>();
        }
    }
}
