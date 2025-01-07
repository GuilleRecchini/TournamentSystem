using TournamentSystem.Application.Dtos;

namespace TournamentSystem.Application.Services
{
    public interface IAuthenticationService
    {
        Task<int> CreateUserAsync(UserRegistrationDto dto);
        Task<UserDto> LoginUserAsync(UserLoginDto dto);
    }
}