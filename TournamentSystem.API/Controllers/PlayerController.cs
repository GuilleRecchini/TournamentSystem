using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Application.Helpers;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPlayerService _playerService;

        public PlayerController(IUserService userService, IPlayerService playerService)
        {
            _userService = userService;
            _playerService = playerService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUserPlayer(PlayerRegistrationDto dto)
        {
            var UserDto = new UserRegistrationDto
            {
                Name = dto.Name,
                Alias = dto.Alias,
                Email = dto.Email,
                Password = dto.Password,
                ConfirmPassword = dto.ConfirmPassword,
                CountryCode = dto.CountryCode,
                Role = UserRole.Player,
            };

            var userId = await _userService.CreateUserAsync(UserDto);

            return StatusCode(
                StatusCodes.Status201Created,
                new { Message = UserRole.Player + " registered successfully.", UserId = userId });
        }


        [Authorize(Roles = "Player")]
        [HttpPost("add-cards")]
        public async Task<IActionResult> AddCardsToCollectionAsync(int[] cardsIds)
        {
            if (cardsIds is null || cardsIds.Length == 0)
                return BadRequest("The list of card IDs cannot be null or empty.");

            var playerId = ClaimsHelper.GetUserId(User);

            var addedCount = await _playerService.AddCardsToCollectionAsync(cardsIds, playerId);

            var message = new StringBuilder();

            if (addedCount < cardsIds.Length)
                message.Append("Some of the selected cards were already in your collection. ");

            message.Append($"{addedCount} card(s) successfully added to your collection.");

            return Ok(new { Message = message.ToString() });
        }

        [Authorize]
        [HttpGet("{playerId}/cards")]
        public async Task<IActionResult> GetCardsByPlayerIdAsyncAsync(int playerId)
        {
            var cards = await _playerService.GetCardsByPlayerIdAsyncAsync(playerId);

            return Ok(cards);
        }
    }
}
