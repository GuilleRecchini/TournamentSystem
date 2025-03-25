using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Application.Dtos
{
    public class UserUpdateDto
    {
        [Required]
        public int UserId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Alias { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(2, MinimumLength = 2)]
        public string CountryCode { get; set; }
    }
}
