using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Dtos
{
    public class TournamentAdminDto : BaseTournamentDto
    {
        public TournamentPhase Phase { get; set; }
        public string PhaseName => Phase.ToString();

        //Navigation properties
        public List<UserForAdminsDto> Players { get; set; }
        public List<UserForAdminsDto> Judges { get; set; }
        public UserForAdminsDto Organizer { get; set; }
        public UserForAdminsDto Winner { get; set; }
    }
}
