namespace Migrator.HTTPClient
{
    public class MyInterceptHttpClient : HttpClient
    {
        public MyInterceptHttpClient(HttpMessageHandler handler)
            : base(handler) { }

        public MyInterceptHttpClient()
            : base(CreateHttpClientHandler()) { }

        private static Func<DelegatingHandler> CreateHttpClientHandler = () =>
        {
            var httpClientHandler = new HttpClientHandler();
            var loggingHandler = new HttpLoggingHandler { InnerHandler = httpClientHandler };

            // Configure the handler as needed
            return loggingHandler;
        };
    }
}
