using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces
{
    public interface ISerieService
    {
        Task<Serie> GetSerieByIdAsync(int id);
        Task<List<Serie>> GetAllSeriesAsync();
    }
}