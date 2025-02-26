using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface ITournamentRepository
    {
        Task<int> CreateTournamentAsync(Tournament t);
        Task<bool> UpdateTournamentAsync(Tournament t);
        Task<Tournament?> GetTournamentByIdAsync(int id, int? organizerId = null, int[]? judgeIds = null, TournamentPhase? phase = null);
        Task<int> AddSeriesToTournamentAsync(int tournamentId, int[] seriesId);
        Task<bool> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds);
        Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId);
        Task<int> GetPlayerCountAsync(int tournamentId);
        Task<bool> FinalizeRegistrationAndStartTournamentAsync(int tournamentId, List<Game> games);
        Task<bool> SetGameWinnerAsync(int gameId, int winnerId);
        Task<bool> DisqualifyPlayerAsync(int playerId, int tournamentId, string reason, int disqualifiedBy);
        Task<bool> AdvanceWinnersToNextRoundAsync(List<Game> games);
        Task<bool> FinalizeTournamentAsync(int tournamentId);
    }
}