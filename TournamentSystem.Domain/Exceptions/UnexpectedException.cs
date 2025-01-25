namespace TournamentSystem.Domain.Exceptions
{
    using System;

    public class UnexpectedException : CustomException
    {
        public UnexpectedException(string message) : base(message)
        {
        }

        public UnexpectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
