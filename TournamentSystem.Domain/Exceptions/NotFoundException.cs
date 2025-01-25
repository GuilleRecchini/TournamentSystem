namespace TournamentSystem.Domain.Exceptions
{
    using System;

    public class NotFoundException : CustomException
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
