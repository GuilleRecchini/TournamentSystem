using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string AvatarUrl { get; set; }
        public string CountryCode { get; set; }
        public UserRole Role { get; set; }
        public int? CreatedBy { get; set; }
    }
}