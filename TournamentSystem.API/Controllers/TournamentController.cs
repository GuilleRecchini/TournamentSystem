using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
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
            await _tournamentService.UpdateTournamentAsync(TournamentUpdate);

            return Ok(new { Message = "Tournament successfully updated." });
        }

        [Authorize]
        [HttpGet("open-for-registration")]
        public async Task<IActionResult> GetTournamentsOpenForRegistrationAsync()
        {
            var userRole = ClaimsHelper.GetUserRole(User);
            var tournaments = await _tournamentService.GetTournamentsByPhaseAsync(TournamentPhase.Registration, userRole);
            return Ok(tournaments);
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
        public async Task<IActionResult> RegisterPlayerAsync(int tournamentId, int[] cardsIds)
        {
            var playerId = ClaimsHelper.GetUserId(User);

            await _tournamentService.RegisterPlayerAsync(tournamentId, playerId, cardsIds);

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

        [Authorize(Roles = nameof(UserRole.Organizer))]
        [HttpPost("{tournamentId}/finalize-registration")]
        public async Task<IActionResult> FinalizeRegistrationAsync(int tournamentId)
        {
            var organizerId = ClaimsHelper.GetUserId(User);

            await _tournamentService.FinalizeRegistrationAsync(tournamentId, organizerId);

            return Ok(new { Message = "Registration successfully finalized. Tournament phase started." });
        }

        [Authorize(Roles = nameof(UserRole.Judge))]
        [HttpPost("{tournamentId}/set-game-winner/{gameId}/{winnerId}")]
        public async Task<IActionResult> SetGameWinnerAsync(int tournamentId, int gameId, int winnerId)
        {
            var judgeId = ClaimsHelper.GetUserId(User);

            var success = await _tournamentService.SetGameWinnerAsync(tournamentId, gameId, judgeId, winnerId);

            return Ok(new { Message = "Game winner successfully set." });
        }

        [Authorize(Roles = nameof(UserRole.Judge))]
        [HttpPost("{tournamentId}/disqualify-player/{playerId}")]
        public async Task<IActionResult> DisqualifyPlayerAsync(int playerId, int tournamentId, DisqualifyPlayerRequest request)
        {
            var judgeId = ClaimsHelper.GetUserId(User);

            await _tournamentService.DisqualifyPlayerAsync(playerId, tournamentId, request.Reason, judgeId);

            return Ok(new { Message = "Player successfully disqualified." });

            throw new NotImplementedException();
        }

        [Authorize(Roles = nameof(UserRole.Player))]
        [HttpPost("{tournamentId}/add-cards/")]
        public async Task<IActionResult> AddCardsToDeck(int tournamentId, int[] cardsIds)
        {
            var playerId = ClaimsHelper.GetUserId(User);

            var addedCount = await _tournamentService.AddCardsToDeckAsync(tournamentId, playerId, cardsIds);

            var message = new StringBuilder();

            if (addedCount < cardsIds.Length)
                message.Append("Some of the selected cards were already in your deck. ");

            message.Append($"{addedCount} card(s) successfully added to your deck.");

            return Ok(new { Message = message.ToString() });
        }

        [Authorize(Roles = nameof(UserRole.Player))]
        [HttpDelete("{tournamentId}/remove-cards/")]
        public async Task<IActionResult> RemoveCardsFromDeck(int tournamentId, int[] cardsIds)
        {
            var playerId = ClaimsHelper.GetUserId(User);

            var removedCount = await _tournamentService.RemoveCardsFromDeckAsync(tournamentId, playerId, cardsIds);

            var message = new StringBuilder();

            if (removedCount < cardsIds.Length)
                message.Append("Some of the selected cards were not in your deck. ");

            message.Append($"{removedCount} card(s) successfully removed from your deck.");

            return Ok(new { Message = message.ToString() });
        }

        [Authorize(Roles = nameof(UserRole.Player))]
        [HttpGet("{tournamentId}/get-deck/")]
        public async Task<IActionResult> GetDeckAsync(int tournamentId)
        {
            var playerId = ClaimsHelper.GetUserId(User);

            var deck = await _tournamentService.GetDeckAsync(playerId, tournamentId);

            return Ok(deck);
        }

        [Authorize(Roles = nameof(UserRole.Administrator))]
        [HttpGet("{tournamentId}/get-all-decks/")]
        public async Task<IActionResult> GetAllDecksAsync(int tournamentId)
        {

            var decks = await _tournamentService.GetTournamentDecksAsyncAsync(tournamentId);

            return Ok(decks);
        }


        [Authorize(Roles = nameof(UserRole.Administrator))]
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllTournamentsAsync()
        {
            var userRole = ClaimsHelper.GetUserRole(User);

            var tournaments = await _tournamentService.GetTournamentsAsync(userRole: userRole);
            return Ok(tournaments);
        }

        [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Organizer)}")]
        [HttpPost("{tournamentId}/cancel")]
        public async Task<IActionResult> CancelTournamentAsync(int tournamentId)
        {
            var userId = ClaimsHelper.GetUserId(User);
            var userRole = ClaimsHelper.GetUserRole(User);

            await _tournamentService.CancelTournamentAsync(tournamentId, userId, userRole);
            return Ok(new { Message = "Tournament successfully canceled." });
        }


        [Authorize(Roles = nameof(UserRole.Administrator))]
        [HttpGet("{tournamentId}/get-all-games")]
        public async Task<IActionResult> GetAllTournamentGamesAsync(int tournamentId)
        {
            var userRole = ClaimsHelper.GetUserRole(User);

            var games = await _tournamentService.GetTournamentGamesAsync(tournamentId);

            return Ok(games);
        }

    }
}
