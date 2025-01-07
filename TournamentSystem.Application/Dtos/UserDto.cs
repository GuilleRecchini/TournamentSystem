using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Dtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public int? CountryId { get; set; }
        public UserRole Role { get; set; }
    }
}
