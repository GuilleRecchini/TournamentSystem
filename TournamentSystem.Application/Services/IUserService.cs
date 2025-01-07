
using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public interface IUserService
    {
        Task<int> CreateUserAsync(UserRegistrationDto dto);
        Task<User> GetUserByIdAsync(int id);
    }
}