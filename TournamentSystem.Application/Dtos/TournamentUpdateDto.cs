namespace TournamentSystem.Application.Dtos
{
    public class TournamentUpdateDto
    {
        public int TournamentId { get; set; }
        public string? Name { get; set; }
        public string? CountryCode { get; set; }
        public int? OrganizerId { get; set; }
        public List<int>? JudgesIds { get; set; }
    }
}
