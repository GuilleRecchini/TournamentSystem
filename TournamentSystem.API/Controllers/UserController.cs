using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.Dtos;
using TournamentSystem.Application.Helpers;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Roles = $"{nameof(UserRole.Administrator)},{nameof(UserRole.Organizer)}")]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(UserRegistrationDto dto)
        {
            var currentUserRol = ClaimsHelper.GetUserRole(User);

            if (currentUserRol == UserRole.Organizer && dto.Role != UserRole.Judge)
            {
                var path = HttpContext.Request.Path;
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails()
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "Organizers can only register users with the 'Judge' role.",
                    Instance = HttpContext.Request.Path
                });
            }

            dto.CreatedBy = ClaimsHelper.GetUserId(User);

            var userId = await _userService.CreateUserAsync(dto);

            return StatusCode(
                StatusCodes.Status201Created,
                new { Message = dto.Role.ToString() + "  registered successfully." });
        }

        [Authorize(Roles = nameof(UserRole.Administrator))]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser(UserUpdateDto dto)
        {
            var updated = await _userService.UpdateUserAsync(dto);

            return Ok(new { Message = "User successfully updated." });
        }

        [Authorize(Roles = nameof(UserRole.Administrator))]
        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var deleted = await _userService.DeleteUserAsync(userId);

            return Ok(new { Message = "User successfully deleted." });
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {

            var userRol = ClaimsHelper.GetUserRole(User);

            var user = await _userService.GetUserByIdAsync(id, userRol);

            return Ok(user);
        }
    }
}
