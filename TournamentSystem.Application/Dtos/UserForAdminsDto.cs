using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Dtos
{
    public class UserForAdminsDto : BaseUserDto
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }
}
