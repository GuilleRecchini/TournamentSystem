using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Application.Dtos
{
    public class PlayerRegistrationDto
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Alias { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(8)]
        public required string Password { get; set; }

        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmPassword { get; set; }

        [Required]
        public required int CountryId { get; set; }
    }
}
