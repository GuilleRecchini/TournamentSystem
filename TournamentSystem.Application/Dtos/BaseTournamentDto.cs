using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Dtos
{
    public class BaseTournamentDto
    {
        public int TournamentId { get; set; }
        public string Name { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int? CountryId { get; set; }
        public int? Winner { get; set; }

        //Navigation properties
        public List<Serie> Series { get; set; }
    }
}