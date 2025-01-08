using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int id);
        Task<int> CreateUserAsync(UserRegistrationDto dto);
    }
}