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

        [Authorize]
        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetTournamentByIdAsync(int tournamentId)
        {
            var userRole = ClaimsHelper.GetUserRole(User);

            var tournament = await _tournamentService.GetTournamentByIdAsync(tournamentId, userRole);

            return Ok(tournament);
        }

        [Authorize(Roles = nameof(UserRole.Player))]
        [HttpPost("{tournamentId}/register")]
        public async Task<IActionResult> RegisterPlayerAsync(int tournamentId)
        {
            var playerId = ClaimsHelper.GetUserId(User);

            var success = await _tournamentService.RegisterPlayerAsync(tournamentId, playerId);

            if (!success)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Registration Error",
                    Detail = "The player could not be registered for the tournament."
                });
            }

            return Ok(new { Message = "Player successfully registered for the tournament." });
        }

        [Authorize(Roles = nameof(UserRole.Organizer))]
        [HttpPost("{tournamentId}/assign-judge/{judgeId}")]
        public async Task<IActionResult> AssignJudgeToTournamentAsync(int tournamentId, int judgeId)
        {
            var organizerId = ClaimsHelper.GetUserId(User);

            var success = await _tournamentService.AssignJudgeToTournamentAsync(tournamentId, judgeId, organizerId);

            return Ok(new { Message = "Judge successfully assigned to the tournament." });
        }

        [Authorize(Roles = nameof(UserRole.Organizer))]
        [HttpPost("{tournamentId}/add-series")]
        public async Task<IActionResult> AddSeriesToToutnamentAsync(int tournamentId, int[] seriesIds)
        {
            var organizerId = ClaimsHelper.GetUserId(User);

            var success = await _tournamentService.AddSeriesToTournamentAsync(tournamentId, seriesIds, organizerId);

            return Ok(new { Message = "Series successfully added to the tournament." });
        }

    }
}
