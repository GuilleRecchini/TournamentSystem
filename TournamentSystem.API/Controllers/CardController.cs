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
            if (card is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Card Error",
                    Detail = "The card does not exist."
                });
            }

            return Ok(card);
        }

        [HttpGet("serie/{serieId}")]
        public async Task<IActionResult> GetCardsBySerieAsync(int serieId)
        {
            var cards = await _cardService.GetCardsBySerieAsync(serieId);

            if (cards is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Card Error",
                    Detail = "The cards do not exist."
                });
            }

            return Ok(cards);

            throw new NotImplementedException();
        }
    }
}
