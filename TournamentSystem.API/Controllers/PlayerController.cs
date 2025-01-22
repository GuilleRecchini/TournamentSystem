using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TournamentSystem.Application.Dtos;
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

            if (userId == -1)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "User Registration Error",
                    Detail = "The email or alias is already in use."
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                new { Message = UserDto.Role.ToString() + " registered successfully." });
        }

        [HttpPost("add-cards")]
        public async Task<IActionResult> AddCardsToCollectionAsync(int[] cardsIds)
        {
            if (cardsIds is null || cardsIds.Length == 0)
                return BadRequest("The list of cards cannot be empty.");

            var playerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(playerIdString))
            {
                return Unauthorized("Player ID is missing or invalid.");
            }

            var playerId = int.Parse(playerIdString);

            var result = await _playerService.AddCardsToCollectionAsync(cardsIds, playerId);

            if (!result)
                return BadRequest();

            return Ok();
        }

        [HttpGet("cards")]
        public async Task<IActionResult> GetCardsByPlayerIdAsyncAsync()
        {
            var playerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(playerIdString))
            {
                return Unauthorized("Player ID is missing or invalid.");
            }

            var playerId = int.Parse(playerIdString);

            var cards = await _playerService.GetCardsByPlayerIdAsyncAsync(playerId);

            return Ok(cards);
        }
    }
}
