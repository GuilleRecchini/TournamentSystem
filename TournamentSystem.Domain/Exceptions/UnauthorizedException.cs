namespace TournamentSystem.Domain.Exceptions
{
    using System;

    public class UnauthorizedException : CustomException
    {
        public UnauthorizedException(string message) : base(message)
        {
        }

        public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
