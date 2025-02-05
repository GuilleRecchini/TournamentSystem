using TournamentSystem.Domain.Entities;

namespace TournamentSystem.DataAccess.Repositories
{
    public interface ISerieRepository
    {
        Task<bool> DoAllSeriesExistAsync(int[] serieIds);
        Task<List<Serie>> GetSeriesAsync(int[] serieIds);
    }
}