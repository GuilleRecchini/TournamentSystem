using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public interface ITournamentService
    {
        Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId);
        Task<bool> UpdateTournamentAsync(TournamentUpdateDto dto);
        Task<Tournament> GetTournamentByIdAsync(int tournamentId);
    }
}