﻿namespace TournamentSystem.Application.Dtos
{
    public class AuthenticationResult
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int? UserId { get; set; }
    }
}
