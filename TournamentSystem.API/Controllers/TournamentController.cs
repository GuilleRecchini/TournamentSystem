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
    public class TournamentController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;

        public TournamentController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }

        [Authorize(Roles = nameof(UserRole.Organizer))]
        [HttpPost("create")]
        public async Task<IActionResult> CreateTournamentAsync(TournamentCreateDto dto)
        {

            var OrganizerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(OrganizerIdString))
            {
                return Unauthorized("Organizer ID is missing or invalid.");
            }

            var tournamentId = await _tournamentService.CreateTournamentAsync(dto, int.Parse(OrganizerIdString));
            return StatusCode(StatusCodes.Status201Created, new { TournamentId = tournamentId });
        }

        [Authorize(Roles = nameof(UserRole.Organizer))]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateTournamentAsync(TournamentUpdateDto dto)
        {
            var success = await _tournamentService.UpdateTournamentAsync(dto);

            if (!success)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Tournament Update Error",
                    Detail = "The tournament could not be updated."
                });
            }

            return Ok(new { Message = "Tournament successfully deleted." });
        }
    }
}
