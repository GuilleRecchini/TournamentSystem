namespace TournamentSystem.Domain.Exceptions
{
    using System;

    public class ValidationException : CustomException
    {
        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
