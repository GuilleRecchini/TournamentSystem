using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Services
{
    public class SerieService : ISerieService
    {
        private readonly ISerieRepository _serieRepository;

        public SerieService(ISerieRepository serieRepository)
        {
            _serieRepository = serieRepository;
        }

        public async Task<Serie> GetSerieByIdAsync(int id)
        {
            var result = await _serieRepository.GetSerieByIdAsync(id);

            if (result is null)
                throw new NotFoundException("Serie not found");

            return result;
        }

        public async Task<List<Serie>> GetAllSeriesAsync()
        {
            var result = await _serieRepository.GetAllSeriesAsync();

            if (result is null)
                throw new NotFoundException("No series found");

            return result;
        }
    }
}
