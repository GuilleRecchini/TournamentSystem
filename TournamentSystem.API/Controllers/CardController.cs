using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.Services;

namespace TournamentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CardController : ControllerBase
    {
        public readonly ICardService _cardService;

        public CardController(ICardService cardService)
        {
            _cardService = cardService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCardByIdAsync(int id)
        {
            var card = await _cardService.GetCardByIdAsync(id);

            return Ok(card);
        }

        [HttpGet("serie/{serieId}")]
        public async Task<IActionResult> GetCardsBySerieAsync(int serieId)
        {
            var cards = await _cardService.GetCardsBySerieAsync(serieId);

            return Ok(cards);
        }
    }
}
