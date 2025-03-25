using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Services
{
    public interface ITournamentService
    {
        Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId);
        Task<bool> UpdateTournamentAsync(TournamentUpdateDto dto);
        Task<BaseTournamentDto> GetTournamentByIdAsync(int tournamentId, UserRole userRole);
        Task<bool> RegisterPlayerAsync(int tournamentId, int playerId, int[] cardsIds);
        Task<bool> AssignJudgeToTournamentAsync(int tournamentId, int judgeId, int organizerId);
        Task<bool> AddSeriesToTournamentAsync(int tournamentId, int[] seriesIds, int organizerId);
        Task<bool> FinalizeRegistrationAsync(int tournamentId, int organizerId);
        Task<bool> SetGameWinnerAsync(int tournamentId, int gameId, int judgeId, int winnerId);
        Task<bool> DisqualifyPlayerAsync(int playerId, int tournamentId, string reason, int judgeId);
        Task<IEnumerable<BaseTournamentDto>> GetTournamentsAsync(UserRole userRole);
        Task<IEnumerable<BaseTournamentDto>> GetTournamentsByPhaseAsync(TournamentPhase phase, UserRole userRole);
        Task<int> AddCardsToDeckAsync(int tournamentId, int playerId, int[] cardsIds);
        Task<int> RemoveCardsFromDeckAsync(int tournamentId, int playerId, int[] cardsIds);
        Task<DeckDto> GetDeckAsync(int playerId, int tournamentId);
        Task<bool> CancelTournamentAsync(int tournamentId, int userId, UserRole userRole);
        Task<IEnumerable<DeckDto>> GetTournamentDecksAsyncAsync(int tournamentId);
        Task<IEnumerable<GameDto>> GetTournamentGamesAsync(int tournamentId);
        Task<IEnumerable<BaseTournamentDto>> GetTournamentsByUserIdAsync(int userId);
        Task<IEnumerable<BaseTournamentDto>> GetTournamentsByWinnerAsync(int userId);
        Task<IEnumerable<GameDto>> GetLostGamesByPlayerIdAsync(int playerId);
        Task<IEnumerable<GameDto>> GetWonGamesByPlayerIdAsync(int playerId);
    }
}