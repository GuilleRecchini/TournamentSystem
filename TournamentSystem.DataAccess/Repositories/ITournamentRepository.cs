using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface ITournamentRepository
    {
        Task<int> CreateTournamentAsync(Tournament t);
        Task<bool> UpdateTournamentAsync(Tournament t);
        Task<Tournament?> GetTournamentByIdAsync(int id);
        Task<int> AddSeriesToTournamentAsync(int tournamentId, int[] seriesId);
    }
}