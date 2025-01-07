using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetUserByIdAsync(id);
        }

        public async Task<int> CreateUserAsync(UserRegistrationDto dto)
        {
            var user = new User
            {
                Name = dto.Name,
                Alias = dto.Alias,
                Email = dto.Email,
                // Hashea la contraseña antes de guardarla
                PasswordHash = dto.Password, //_passwordHasher.HashPassword(dto.Password),
                CountryId = dto.CountryId,
                Role = dto.Role,
            };

            return await _userRepository.CreateUserAsync(user);
        }
    }
}
