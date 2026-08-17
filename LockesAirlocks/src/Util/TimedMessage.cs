using System;

namespace IngameScript
{
    /// <summary>A short status/error message shown on the PB detail panel until it expires.</summary>
    public class TimedMessage
    {
        public readonly TimeSpan Expiration;
        public readonly string Message;

        public TimedMessage(TimeSpan expiration, string message)
        {
            Expiration = expiration;
            Message = message;
        }
    }
}
