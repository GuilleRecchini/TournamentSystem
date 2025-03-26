using TournamentSystem.Application.Dtos;

namespace TournamentSystem.Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> LoginUserAsync(UserLoginDto dto);

        Task<AuthenticationResult> RefreshTokensAsync(RefreshTokenDto dto);

        Task LogoutUserAsync(string refreshToken);
    }
}