namespace TournamentSystem.Application.Dtos
{
    public class GameDto
    {
        public int GameId { get; set; }
        public int TournamentId { get; set; }
        public DateTime StartTime { get; set; }

        // Navigation properties

        public BaseUserDto? Player1 { get; set; }
        public BaseUserDto? Player2 { get; set; }
        public BaseUserDto? Winner { get; set; }
    }
}