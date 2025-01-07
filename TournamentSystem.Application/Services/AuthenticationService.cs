using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Security;

namespace TournamentSystem.Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _authenticationRepository;

        public AuthenticationService(IAuthenticationRepository authenticationRepository)
        {
            _authenticationRepository = authenticationRepository;
        }

        public async Task<int> CreateUserAsync(UserRegistrationDto dto)
        {
            var hashedPassword = PasswordHasher.HashPassword(dto.Password);

            var user = new User
            {
                Name = dto.Name,
                Alias = dto.Alias,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                CountryId = dto.CountryId,
                Role = dto.Role,
            };

            return await _authenticationRepository.CreateUserAsync(user);
        }

        public async Task<UserDto> LoginUserAsync(UserLoginDto dto)
        {
            var user = await _authenticationRepository.GetUserByEmail(dto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("El email o la contraseña son incorrectos. Por favor, inténtelo nuevamente.");

            var passwordMatch = PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash);

            if (!passwordMatch)
                throw new UnauthorizedAccessException("El email o la contraseña son incorrectos. Por favor, inténtelo nuevamente.");

            return new UserDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Alias = user.Alias,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                CountryId = user.CountryId,
                Role = user.Role,
            };
        }
    }
}
