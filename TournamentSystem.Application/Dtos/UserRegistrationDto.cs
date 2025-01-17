using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Dtos
{
    public class UserRegistrationDto : PlayerRegistrationDto
    {
        [Required]
        [EnumDataType(typeof(UserRole), ErrorMessage = "Invalid role. Please select a valid role.")]
        public UserRole Role { get; set; }

        public int? CreatedBy { get; set; }
    }
}