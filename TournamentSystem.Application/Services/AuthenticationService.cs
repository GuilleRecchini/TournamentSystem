using TournamentSystem.Application.Dtos;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.DataAccess.Interfaces;
using TournamentSystem.Domain.Exceptions;
using TournamentSystem.Infrastructure.Security;

namespace TournamentSystem.Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IUserRepository _userRepository;
        private readonly TokenProvider _tokenProvider;

        public AuthenticationService(IAuthenticationRepository authenticationRepository, IUserRepository userRepository, TokenProvider tokenProvider)
        {
            _authenticationRepository = authenticationRepository;
            _userRepository = userRepository;
            _tokenProvider = tokenProvider;
        }

        public async Task<AuthenticationResult> LoginUserAsync(UserLoginDto dto)
        {
            var user = await _userRepository.GetUserByEmailAsync(dto.Email);

            if (user == null)
                throw new UnauthorizedException("Invalid credentials.");

            var passwordMatch = PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash);

            if (!passwordMatch)
                throw new UnauthorizedException("Invalid credentials.");

            var accessToken = _tokenProvider.CreateJwt(user);
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.UserId);

            return new AuthenticationResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.UserId
            };
        }

        public async Task<AuthenticationResult> RefreshTokensAsync(RefreshTokenDto dto)
        {
            var existingToken = await _authenticationRepository.GetRefreshTokenAsync(dto.RefreshToken);

            if (existingToken is null)
                throw new UnauthorizedException("Invalid or expired refresh token.");

            var user = await _userRepository.GetUserByIdAsync(existingToken.UserId);

            if (user == null || user.UserId != dto.UserId)
                throw new UnauthorizedException("Invalid user.");

            var accessToken = _tokenProvider.CreateJwt(user);

            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.UserId);

            return new AuthenticationResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.UserId
            };
        }

        public async Task LogoutUserAsync(string refreshToken)
        {
            await _authenticationRepository.DeleteRefreshTokenAsync(refreshToken);
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(int userId)
        {
            var refreshToken = _tokenProvider.GenerateRefreshToken(userId);

            await _authenticationRepository.AddRefreshTokenAsync(refreshToken);

            return refreshToken.Token;
        }
    }
}
