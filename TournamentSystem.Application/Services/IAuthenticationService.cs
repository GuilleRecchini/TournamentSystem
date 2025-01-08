using TournamentSystem.Application.Dtos;

namespace TournamentSystem.Application.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> LoginUserAsync(UserLoginDto dto);

        Task<AuthenticationResult> RefreshTokensAsync(RefreshTokenDto dto);
    }
}