using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public interface ISerieService
    {
        Task<Serie> GetSerieByIdAsync(int id);
        Task<List<Serie>> GetAllSeriesAsync();
    }
}