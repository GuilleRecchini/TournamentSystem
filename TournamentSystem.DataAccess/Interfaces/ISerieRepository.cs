using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Interfaces
{
    public interface ISerieRepository
    {
        Task<bool> DoAllSeriesExistAsync(int[] serieIds);
        Task<List<Serie>> GetSeriesByIdsAsync(int[] serieIds);
        Task<List<Serie>> GetAllSeriesAsync();
        Task<Serie> GetSerieByIdAsync(int serieId);
    }
}