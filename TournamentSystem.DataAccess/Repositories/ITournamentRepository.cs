using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface ITournamentRepository
    {
        Task<int> CreateTournamentAsync(Tournament t);
        Task<bool> UpdateTournamentAsync(Tournament t);
        Task<int> AddSeriesToTournamentAsync(int tournamentId, int[] seriesId);
        Task<User> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds);
        Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId);
        Task<int> GetPlayerCountAsync(int tournamentId);
        Task<bool> FinalizeRegistrationAndStartTournamentAsync(int tournamentId, List<Game> games);
        Task<bool> SetGameWinnerAsync(int gameId, int winnerId);
        Task<bool> DisqualifyPlayerAsync(int playerId, int tournamentId, string reason, int disqualifiedBy);
        Task<bool> AdvanceWinnersToNextRoundAsync(List<Game> games);
        Task<bool> FinalizeTournamentAsync(int tournamentId);
        //Task<IEnumerable<Tournament>> GetTournamentsAsync(int? id = null, int? organizerId = null, int[]? judgeIds = null, TournamentPhase? phase = null, bool? isCanceled = null);
        Task<IEnumerable<Tournament>> GetTournamentsAsync(TournamentsFilter? filter = null);
        Task<Tournament?> GetTournamentByIdAsync(int tournamnetId, TournamentsFilter? filter = null);
        Task<Deck?> GetPlayerDeckByTournamentIdAsync(int tournamentId, int playerId);
        Task<int> AddCardsToDeckAsync(int deckId, int[] cardsIds);
        Task<int> RemoveCardsFromDeckAsync(int deckId, int[] cardsIds);
        Task<bool> CancelTournamentAsync(int tournamentId);
        Task<IEnumerable<Deck>> GetTournamentDecksAsync(int tournamentId);
    }
}