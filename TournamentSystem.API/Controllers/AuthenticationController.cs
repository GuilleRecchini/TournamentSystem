using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Application.Helpers;
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
                Message = "Authentication successful.",
                authResult.UserId,
                authResult.AccessToken,
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokensAsync()
        {
            var userId = ClaimsHelper.GetUserId(User);

            var refreshToken = Request.Cookies["RefreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Refresh token is missing."
                });
            }

            var refreshTokenDto = new RefreshTokenDto()
            {
                UserId = userId,
                RefreshToken = refreshToken
            };

            var authResult = await _authenticationService.RefreshTokensAsync(refreshTokenDto);

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
                Message = "Tokens refreshed successfully.",
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
