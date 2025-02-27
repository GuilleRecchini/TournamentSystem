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
        Task<IEnumerable<BaseTournamentDto>> GetTournamentsAsync(TournamentPhase registration, UserRole userRole);
    }
}