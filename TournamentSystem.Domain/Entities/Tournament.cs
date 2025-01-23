using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Domain.Entities
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int? CountryId { get; set; }
        public TournamentPhase Phase { get; set; }
        public int? Winner { get; set; }
        public int? OrganizerId { get; set; }
    }
}
