namespace TournamentSystem.Domain.Exceptions
{
    using System;

    public class ConflictException : CustomException
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
