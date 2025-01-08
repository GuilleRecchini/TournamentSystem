namespace TournamentSystem.Application.Dtos
{
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public int? UserId { get; set; }
        public string Message { get; set; }
    }
}
