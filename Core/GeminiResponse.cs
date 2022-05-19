namespace GeminiCurl
{
    public class GeminiResponse : IDisposable
    {
        public int StatusCode { get; }
        public string Meta { get; }
        public Stream Content { get; }

        public GeminiResponse(int statusCode, string meta, Stream content)
        {
            StatusCode = statusCode;
            Meta = meta;
            Content = content;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Content.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool disposedValue;
    }
}