using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Application.Services;
using TournamentSystem.Infrastructure.Configurations;

namespace TournamentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IOptions<JwtOptions> _jwtOptions;

        public AuthenticationController(IAuthenticationService authenticationService, IOptions<JwtOptions> jwtOptions)
        {
            _authenticationService = authenticationService;
            _jwtOptions = jwtOptions;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUserAsync([FromBody] UserLoginDto dto)
        {
            var authResult = await _authenticationService.LoginUserAsync(dto);

            if (!authResult.Success)
                return Unauthorized(new { authResult.Message });

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenExpirationDays)
            };

            Response.Cookies.Append("RefreshToken", authResult.RefreshToken, cookieOptions);

            return Ok(new
            {
                authResult.Message,
                authResult.UserId,
                authResult.AccessToken,
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokensAsync()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var parsedUserId))
                return Unauthorized("User ID not found or invalid.");

            var refreshToken = Request.Cookies["RefreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Refresh token is missing.");

            var refreshTokenDto = new RefreshTokenDto()
            {
                UserId = parsedUserId,
                RefreshToken = refreshToken
            };

            var authResult = await _authenticationService.RefreshTokensAsync(refreshTokenDto);

            if (!authResult.Success)
                return Unauthorized(new { authResult.Message });

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenExpirationDays)
            };

            Response.Cookies.Append("RefreshToken", authResult.RefreshToken, cookieOptions);

            return Ok(new
            {
                authResult.Message,
                authResult.UserId,
                authResult.AccessToken,
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> LogoutAsync()
        {
            var refreshToken = Request.Cookies["RefreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
                await _authenticationService.LogoutUserAsync(refreshToken);

            Response.Cookies.Delete("RefreshToken");

            return Ok(new { Message = "Logout successful." });
        }
    }
}
