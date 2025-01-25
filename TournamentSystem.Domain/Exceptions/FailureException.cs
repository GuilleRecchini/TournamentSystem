namespace TournamentSystem.Domain.Exceptions
{
    using System;

    public class FailureException : CustomException
    {
        public FailureException(string message) : base(message)
        {
        }

        public FailureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
