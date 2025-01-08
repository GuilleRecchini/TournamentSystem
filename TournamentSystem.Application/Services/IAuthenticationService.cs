using TournamentSystem.Application.Dtos;

namespace TournamentSystem.Application.Services
{
    public interface IAuthenticationService
    {
        Task<int> CreateUserAsync(UserRegistrationDto dto);
        Task<AuthenticationResult> LoginUserAsync(UserLoginDto dto);
    }
}