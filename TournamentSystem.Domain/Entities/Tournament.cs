using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Domain.Entities
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string CountryCode { get; set; }
        public TournamentPhase Phase { get; set; }
        public int? WinnerId { get; set; }
        public int? OrganizerId { get; set; }
        public bool IsCanceled { get; set; }
        public int MaxPlayers { get; set; }

        //Navigation properties
        public List<Serie> Series { get; set; }
        public List<User> Players { get; set; }
        public List<User> Judges { get; set; }
        public User Organizer { get; set; }
        public User Winner { get; set; }
        public List<Game> Games { get; set; }
    }
}
