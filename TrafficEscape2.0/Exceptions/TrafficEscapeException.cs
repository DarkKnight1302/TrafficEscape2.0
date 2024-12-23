namespace TrafficEscape2._0.Exceptions
{
    public class TrafficEscapeException : Exception
    {
        public int ErrorCode { get; }

        public TrafficEscapeException(string message, int errorCode) : base(message)
        {
            this.ErrorCode = errorCode;
        }
    }
}
