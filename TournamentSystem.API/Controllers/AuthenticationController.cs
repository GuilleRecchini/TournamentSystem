using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Application.Services;

namespace TournamentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {

        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationDto dto)
        {
            var userId = await _authenticationService.CreateUserAsync(dto);

            return Ok(new { UserId = userId });
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] UserLoginDto dto)
        {
            var authResult = await _authenticationService.LoginUserAsync(dto);

            if (!authResult.Success)
                return Unauthorized(new { authResult.Message });

            return Ok(new
            {
                authResult.Message,
                authResult.UserId,
                authResult.AccessToken,
            });
        }
    }
}
