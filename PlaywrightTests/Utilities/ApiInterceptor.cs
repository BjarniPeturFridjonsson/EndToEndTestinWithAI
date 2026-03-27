namespace PlaywrightTests.Utilities
{
    /// <summary>
    /// Captures outgoing HTTP requests matching a URL pattern while letting them continue normally.
    /// Use in a 'await using' block — stops interception automatically on dispose.
    ///
    /// Example:
    ///   await using var interceptor = await ApiInterceptor.StartAsync(Page, "**/api/flights/**");
    ///   await Page.ClickAsync("button#save");
    ///   Assert.IsTrue(interceptor.Requests.Any(r => r.Method == "POST"));
    /// </summary>
    public class ApiInterceptor : IAsyncDisposable
    {
        private readonly IPage             _page;
        private readonly string            _urlPattern;
        private readonly List<IRequest>    _requests = new();

        private ApiInterceptor(IPage page, string urlPattern)
        {
            _page       = page;
            _urlPattern = urlPattern;
        }

        public static async Task<ApiInterceptor> StartAsync(IPage page, string urlPattern)
        {
            var interceptor = new ApiInterceptor(page, urlPattern);

            await page.RouteAsync(urlPattern, async route =>
            {
                interceptor._requests.Add(route.Request);
                await route.ContinueAsync();
            });

            return interceptor;
        }

        public IReadOnlyList<IRequest> Requests => _requests;

        public bool AnyPost()   => _requests.Any(r => r.Method == "POST");
        public bool AnyGet()    => _requests.Any(r => r.Method == "GET");
        public bool AnyDelete() => _requests.Any(r => r.Method == "DELETE");

        public async ValueTask DisposeAsync()
        {
            await _page.UnrouteAsync(_urlPattern);
        }
    }
}
