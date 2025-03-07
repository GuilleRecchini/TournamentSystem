using TournamentSystem.Domain.Enums;

namespace TournamentSystem.Application.Dtos
{
    public class TournamentsFilter
    {
        public int? OrganizerId { get; set; }
        public int[]? JudgeIds { get; set; }
        public TournamentPhase? Phase { get; set; }
        public bool? IsCanceled { get; set; }
    }

}
