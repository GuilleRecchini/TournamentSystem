using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Exceptions;
using TournamentSystem.Infrastructure.Security;

namespace TournamentSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<int> CreateUserAsync(UserRegistrationDto dto)
        {
            var userExist = await _userRepository.UserExistsByEmailOrAlias(dto.Email, dto.Alias);

            if (userExist)
                throw new ValidationException("The email or alias is already in use.");

            var hashedPassword = PasswordHasher.HashPassword(dto.Password);

            var user = new User
            {
                Name = dto.Name,
                Alias = dto.Alias,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                //AvatarUrl  Debería establecer una imagen por defecto que luego se pueda modificar
                CountryCode = dto.CountryCode,
                Role = dto.Role,
                CreatedBy = dto.CreatedBy
            };

            return await _userRepository.CreateUserAsync(user);
        }

        public async Task<bool> UpdateUserAsync(UserUpdateDto dto)
        {
            var user = await _userRepository.GetUserByIdAsync(dto.UserId);

            if (user is null)
                throw new NotFoundException("The user does not exist.");

            if (dto.Name != null) user.Name = dto.Name;
            if (dto.Alias != null) user.Alias = dto.Alias;
            if (dto.Email != null) user.Email = dto.Email;
            if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;
            if (dto.CountryCode != null) user.CountryCode = dto.CountryCode;

            return await _userRepository.UpdateUserAsync(user);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user is null)
                throw new NotFoundException("The user does not exist.");

            return await _userRepository.DeleteUserAsync(userId);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetUserByIdAsync(id);
        }
    }
}
