using AutoMapper;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Helpers
{
    public class TournamentMappingProfile : Profile
    {
        public TournamentMappingProfile()
        {
            CreateMap<Tournament, TournamentAdminDto>();

            CreateMap<Tournament, TournamentPublicDto>();

            CreateMap<Tournament, BaseTournamentDto>();
        }
    }
}
