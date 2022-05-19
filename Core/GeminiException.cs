namespace GeminiCurl
{
    public sealed class GeminiException : Exception
    {
        public GeminiException()
        {
        }

        public GeminiException(string? message) : base(message)
        {
        }

        public GeminiException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}