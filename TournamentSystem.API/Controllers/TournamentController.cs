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
    public class TournamentController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;

        public TournamentController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }

        [Authorize(Roles = nameof(UserRole.Organizer))]
        [HttpPost("create")]
        public async Task<IActionResult> CreateTournamentAsync(TournamentCreateDto tournamentCreate)
        {
            var OrganizerId = ClaimsHelper.GetUserId(User);

            var tournamentId = await _tournamentService.CreateTournamentAsync(tournamentCreate, OrganizerId);

            return StatusCode(StatusCodes.Status201Created, new { TournamentId = tournamentId });
        }

        [Authorize(Roles = nameof(UserRole.Organizer))]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateTournamentAsync(TournamentUpdateDto TournamentUpdate)
        {
            var success = await _tournamentService.UpdateTournamentAsync(TournamentUpdate);

            if (!success)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Tournament Update Error",
                    Detail = "The tournament could not be updated."
                });
            }

            return Ok(new { Message = "Tournament successfully updated." });
        }
    }
}
