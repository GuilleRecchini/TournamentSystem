using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Application.Helpers;
using TournamentSystem.Application.Services;
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
                CountryId = dto.CountryId,
                Role = UserRole.Player,
            };

            var userId = await _userService.CreateUserAsync(UserDto);

            return StatusCode(
                StatusCodes.Status201Created,
                new { Message = UserRole.Player + " registered successfully.", UserId = userId });
        }

        [HttpPost("add-cards")]
        public async Task<IActionResult> AddCardsToCollectionAsync(int[] cardsIds)
        {
            if (cardsIds is null || cardsIds.Length == 0)
                return BadRequest("The list of cards cannot be empty.");

            var playerId = ClaimsHelper.GetUserId(User);

            var addedCount = await _playerService.AddCardsToCollectionAsync(cardsIds, playerId);

            return Ok(new { Message = addedCount + " card(s) successfully added." });
        }

        [HttpGet("cards")]
        public async Task<IActionResult> GetCardsByPlayerIdAsyncAsync()
        {
            var playerId = ClaimsHelper.GetUserId(User);

            var cards = await _playerService.GetCardsByPlayerIdAsyncAsync(playerId);

            return Ok(cards);
        }
    }
}
