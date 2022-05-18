namespace GeminiCurl
{
    public class GeminiResponse
    {
        public int StatusCode { get; }
        public string Meta { get; }
        public string Content { get; }

        public GeminiResponse(int statusCode, string meta, string content)
        {
            StatusCode = statusCode;
            Meta = meta;
            Content = content;
        }
    }
}