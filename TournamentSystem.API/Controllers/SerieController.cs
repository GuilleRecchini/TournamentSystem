using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.Services;

namespace TournamentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SerieController : ControllerBase
    {
        private readonly ISerieService _serieService;

        public SerieController(ISerieService serieService)
        {
            _serieService = serieService;
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSerieByIdAsync(int id)
        {
            var serie = await _serieService.GetSerieByIdAsync(id);
            return Ok(serie);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllSeriesAsync()
        {
            var series = await _serieService.GetAllSeriesAsync();
            return Ok(series);
        }
    }
}
