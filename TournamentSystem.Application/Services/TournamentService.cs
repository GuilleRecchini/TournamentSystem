using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;

        public TournamentService(ITournamentRepository tournamentRepository)
        {
            _tournamentRepository = tournamentRepository;
        }


        public async Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId)
        {
            var tournament = new Tournament
            {
                Name = dto.Name,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                CountryId = dto.CountryId,
                OrganizerId = oganizerId
            };

            return await _tournamentRepository.CreateTournamentAsync(tournament);
        }

        public async Task<bool> UpdateTournamentAsync(TournamentUpdateDto dto)
        {
            var existingTournament = await _tournamentRepository.GetTournamentByIdAsync(dto.TournamentId);

            if (existingTournament is null)
                throw new NotFoundException("Tournament not found");

            existingTournament.Name = dto.Name ?? existingTournament.Name;
            existingTournament.StartDateTime = dto.StartDateTime ?? existingTournament.StartDateTime;
            existingTournament.EndDateTime = dto.EndDateTime ?? existingTournament.EndDateTime;
            existingTournament.CountryId = dto.CountryId ?? existingTournament.CountryId;
            existingTournament.Winner = dto.Winner ?? existingTournament.Winner;
            existingTournament.OrganizerId = dto.OrganizerId ?? existingTournament.OrganizerId;

            return await _tournamentRepository.UpdateTournamentAsync(existingTournament);
        }
    }
}
