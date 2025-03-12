namespace TournamentSystem.Domain.Entities
{
    public class Game
    {
        public int GameId { get; set; }
        public int TournamentId { get; set; }
        public DateTime StartTime { get; set; }
        public int? Player1Id { get; set; }
        public int? Player2Id { get; set; }
        public int? WinnerId { get; set; }

        // Navigation properties

        public User? Player1 { get; set; }
        public User? Player2 { get; set; }
        public User? Winner { get; set; }
    }
}
