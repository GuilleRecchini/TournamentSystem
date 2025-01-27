using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ISerieRepository _serieRepository;

        public TournamentService(ITournamentRepository tournamentRepository, ISerieRepository serieRepository)
        {
            _tournamentRepository = tournamentRepository;
            _serieRepository = serieRepository;
        }


        public async Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId)
        {
            var seriesExist = await _serieRepository.DoAllSeriesExistAsync(dto.SeriesIds.ToArray());

            if (!seriesExist)
                throw new NotFoundException("One or more series do not exist");

            var tournament = new Tournament
            {
                Name = dto.Name,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                CountryId = dto.CountryId,
                OrganizerId = oganizerId
            };
            var tournamentId = await _tournamentRepository.CreateTournamentAsync(tournament);

            await _tournamentRepository.AddSeriesToTournamentAsync(tournamentId, dto.SeriesIds.ToArray());

            return tournamentId;
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

        public async Task<Tournament> GetTournamentByIdAsync(int tournamentId)
        {
            var tournament = await _tournamentRepository.GetTournamentByIdAsync(tournamentId);

            if (tournament is null)
                throw new NotFoundException("Tournament not found");

            return tournament;
        }
    }
}
