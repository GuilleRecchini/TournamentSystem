using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
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
            var user = await _userRepository.GetUserByEmail(dto.Email);

            if (user == null)
                return new AuthenticationResult { Success = false, Message = "Invalid credentials." };

            var passwordMatch = PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash);

            if (!passwordMatch)
                return new AuthenticationResult { Success = false, Message = "Invalid credentials." };

            var accessToken = _tokenProvider.CreateJwt(user);
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.UserId);

            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.UserId,
                Message = "Authentication successful."
            };
        }

        public async Task<AuthenticationResult> RefreshTokensAsync(RefreshTokenDto dto)
        {

            var existingToken = await _authenticationRepository.GetRefreshTokenAsync(dto.RefreshToken);

            if (existingToken == null)
            {
                return new AuthenticationResult { Success = false, Message = "Invalid or expired refresh token." };
            }

            var user = await _userRepository.GetUserByIdAsync(existingToken.UserId);

            if (user == null || user.UserId != dto.UserId)
            {
                return new AuthenticationResult { Success = false, Message = "User not found." };
            }

            var accessToken = _tokenProvider.CreateJwt(user);

            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.UserId);

            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.UserId,
                Message = "Tokens refreshed successfully."
            };
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(int userId)
        {
            var refreshToken = _tokenProvider.GenerateRefreshToken(userId);

            await _authenticationRepository.AddRefreshTokenAsync(refreshToken);

            return refreshToken.Token;
        }
    }
}
