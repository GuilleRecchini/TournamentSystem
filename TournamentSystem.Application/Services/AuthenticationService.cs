using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Security;

namespace TournamentSystem.Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly TokenProvider _tokenProvider;

        public AuthenticationService(IAuthenticationRepository authenticationRepository, TokenProvider tokenProvider)
        {
            _authenticationRepository = authenticationRepository;
            _tokenProvider = tokenProvider;
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

        public async Task<AuthenticationResult> LoginUserAsync(UserLoginDto dto)
        {
            var user = await _authenticationRepository.GetUserByEmail(dto.Email);

            if (user == null)
                return new AuthenticationResult { Success = false, Message = "Invalid credentials." };

            var passwordMatch = PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash);

            if (!passwordMatch)
                return new AuthenticationResult { Success = false, Message = "Invalid credentials." };

            var accessToken = _tokenProvider.CreateJwt(user);

            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                UserId = user.UserId,
                Message = "Authentication successful."
            };
        }
    }
}
