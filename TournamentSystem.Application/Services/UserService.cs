using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using TournamentSystem.Application.Dtos;
using TournamentSystem.DataAccess.Repositories;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Domain.Enums;
using TournamentSystem.Domain.Exceptions;
using TournamentSystem.Infrastructure.Security;

namespace TournamentSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;

        public UserService(IUserRepository userRepository, IMapper mapper, IWebHostEnvironment environment)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _environment = environment;
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
                AvatarUrl = "/avatars/default-avatar.png",
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
            if (dto.CountryCode != null) user.CountryCode = dto.CountryCode;

            return await _userRepository.UpdateUserAsync(user);
        }

        public async Task<bool> UploadAvatarAsync(int userId, IFormFile avatar)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(avatar.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ValidationException("Invalid file type. Only JPG and PNG are allowed.");

            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (avatar.Length > maxFileSize)
                throw new ValidationException("File size exceeds the 5MB limit.");

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                throw new ValidationException("User not found.");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Eliminar avatar anterior si no es el avatar por defecto
            if (!string.IsNullOrEmpty(user.AvatarUrl) &&
                !user.AvatarUrl.Contains("default-avatar.png"))
            {
                var oldAvatarPath = Path.Combine(_environment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldAvatarPath))
                    System.IO.File.Delete(oldAvatarPath);
            }

            // Guardar nueva imagen
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(fileStream);
            }

            user.AvatarUrl = $"/avatars/{fileName}";

            var updateResult = await _userRepository.UpdateUserAsync(user);
            if (!updateResult)
                throw new ValidationException("Error updating user avatar.");

            return updateResult;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user is null)
                throw new NotFoundException("The user does not exist.");

            return await _userRepository.DeleteUserAsync(userId);
        }

        public async Task<BaseUserDto> GetUserByIdAsync(int id, UserRole userRole)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            if (user is null)
                throw new NotFoundException("The user does not exist.");

            return userRole == UserRole.Administrator || userRole == UserRole.Organizer
                ? _mapper.Map<UserForAdminsDto>(user)
                : _mapper.Map<BaseUserDto>(user);
        }
    }
}
