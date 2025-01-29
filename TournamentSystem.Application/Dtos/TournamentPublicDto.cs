namespace TournamentSystem.Application.Dtos
{
    public class TournamentPublicDto : BaseTournamentDto
    {
        public List<BaseUserDto> Players { get; set; }
        public List<BaseUserDto> Judges { get; set; }
        public BaseUserDto Organizer { get; set; }
    }
}
