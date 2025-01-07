using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Dtos
{
    public class UserRegistrationDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string Alias { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public int CountryId { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }
}