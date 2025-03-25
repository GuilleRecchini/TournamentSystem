using Microsoft.AspNetCore.Http;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Services
{
    public interface IUserService
    {
        Task<BaseUserDto> GetUserByIdAsync(int id, UserRole userRole);
        Task<int> CreateUserAsync(UserRegistrationDto dto);
        Task<bool> UpdateUserAsync(UserUpdateDto dto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> UploadAvatarAsync(int userId, IFormFile avatar);
    }
}