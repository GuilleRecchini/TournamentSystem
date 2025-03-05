using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Dtos
{
    public class DeckDto
    {
        public int DeckId { get; set; }
        public int PlayerId { get; set; }
        public int TournamentId { get; set; }

        // Navigation properties
        public List<Card> Cards { get; set; }
    }
}
