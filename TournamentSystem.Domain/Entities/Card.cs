namespace TournamentSystem.Domain.Entities
{
    public class Card
    {
        public int CardId { get; set; }
        public string Name { get; set; }
        public string Illustration { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
    }
}
