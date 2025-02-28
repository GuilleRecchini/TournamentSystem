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

        [Required]
        public string CountryCode { get; set; }

        [Required]
        public List<int> SeriesIds { get; set; }

        [Required]
        public List<int> JudgesIds { get; set; }
    }
}
