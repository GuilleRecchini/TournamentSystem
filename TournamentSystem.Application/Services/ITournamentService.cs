using TournamentSystem.Application.Dtos;

namespace TournamentSystem.Application.Services
{
    public interface ITournamentService
    {
        Task<int> CreateTournamentAsync(TournamentCreateDto dto, int oganizerId);
        Task<bool> UpdateTournamentAsync(TournamentUpdateDto dto);
    }
}