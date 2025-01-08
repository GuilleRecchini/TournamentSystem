namespace TournamentSystem.Infrastructure.Configurations
{
    public class JwtOptions
    {
        public string Secret { get; set; }
        public int ExpirationMinutes { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
