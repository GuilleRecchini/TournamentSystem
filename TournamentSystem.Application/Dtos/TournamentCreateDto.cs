using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Application.Dtos
{
    public class TournamentCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        public int? CountryId { get; set; }
        public List<int> SeriesIds { get; set; }
    }
}
