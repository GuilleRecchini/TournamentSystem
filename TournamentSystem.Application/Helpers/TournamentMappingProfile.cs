using AutoMapper;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;
using static TournamentSystem.Application.Helpers.TournamentServiceHelpers;

namespace TournamentSystem.Application.Helpers
{
    public class TournamentMappingProfile : Profile
    {
        public TournamentMappingProfile()
        {
            CreateMap<Tournament, TournamentAdminDto>()
                .ForMember(dest => dest.MaxPlayers, opt => opt.MapFrom(src => CalculateMaxPlayers(src)));

            CreateMap<Tournament, TournamentPublicDto>()
                .ForMember(dest => dest.MaxPlayers, opt => opt.MapFrom(src => CalculateMaxPlayers(src)));

            CreateMap<Tournament, BaseTournamentDto>()
                .ForMember(dest => dest.MaxPlayers, opt => opt.MapFrom(src => CalculateMaxPlayers(src)));

        }
    }
}
