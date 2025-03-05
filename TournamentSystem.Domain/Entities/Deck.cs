namespace TournamentSystem.Domain.Entities
{
    public class Deck
    {
        public int DeckId { get; set; }

        // Navigation properties
        public User Player { get; set; }
        public Tournament Tournament { get; set; }
        public List<Card> Cards { get; set; }
    }
}
