using System.Reflection;
using Deque.AxeCore.Playwright;

namespace PlaywrightTests.Setup
{
    /// <summary>
    /// Base class for all authenticated tests.
    ///
    /// SHARED_CONTEXT=1  — one IBrowserContext per role, shared across all tests.
    ///                     Session state and cookies persist between tests (good for finding leaks).
    ///                     Tests run sequentially. Each test gets its own page (tab).
    ///
    /// Default           — each test creates and owns its own IBrowserContext.
    ///                     Tests are fully isolated and safe to run in parallel.
    ///                     Use with: dotnet test --settings PlaywrightTests/parallel.runsettings
    /// </summary>
    public abstract class AuthenticatedPageTest
    {
        protected virtual string AuthStateFile => TestUsers.Super.AuthStateFile;

        internal static readonly string ArtifactDir =
            Path.Combine(AppContext.BaseDirectory, "artifacts");

        private static bool IsSharedContext => Environment.GetEnvironmentVariable("SHARED_CONTEXT") == "1";

        internal static bool HasFailures { get; private set; }

        private IBrowserContext _context = null!;
        private bool _ownedContext;

        public TestContext TestContext { get; set; } = null!;

        protected IBrowserContext Context => _context;
        protected IPage Page { get; private set; } = null!;

        public static IPageAssertions    Expect(IPage page)       => Assertions.Expect(page);
        public static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

        [TestInitialize]
        public async Task StartTracing()
        {
            Directory.CreateDirectory(ArtifactDir);

            if (IsSharedContext)
            {
                _context     = GlobalSetup.SharedContexts[AuthStateFile];
                _ownedContext = false;
            }
            else
            {
                _context = await GlobalSetup.SharedBrowser.NewContextAsync(new()
                {
                    StorageStatePath  = AuthStateFile,
                    IgnoreHTTPSErrors = true,
                    RecordVideoDir    = ArtifactDir,
                    RecordVideoSize   = new RecordVideoSize { Width = 1280, Height = 720 }
                });
                _ownedContext = true;
            }

            Page = await _context.NewPageAsync();
            await _context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots   = true,
                Sources     = true
            });
        }

        [TestCleanup]
        public async Task CollectArtifactsAndRecordResult()
        {
            var failed    = TestContext.CurrentTestOutcome != UnitTestOutcome.Passed;
            var tracePath = Path.Combine(ArtifactDir, $"{TestContext.TestName}.trace.zip");

            if (failed)
                await _context.Tracing.StopAsync(new() { Path = tracePath });
            else
            {
                await _context.Tracing.StopAsync(new());
                tracePath = null;
            }

            var videoPath = Page.Video != null ? await Page.Video.PathAsync() : null;

            if (failed && Environment.GetEnvironmentVariable("PAUSE_ON_FAIL") == "1")
            {
                HasFailures = true;
                Console.WriteLine($"\nTEST FAILED: {TestContext.TestName}");
                Console.WriteLine("Browser tab left open for inspection.");
                // Leave the page open so the tab stays visible until AssemblyCleanup.
            }
            else
            {
                await Page.CloseAsync();
            }

            if (_ownedContext)
                await _context.CloseAsync();

            TestResultCollector.RecordRun(TestContext.TestName ?? "", TestContext.CurrentTestOutcome, videoPath);

            var method     = GetType().GetMethod(TestContext.TestName ?? "");
            var attributes = method?.GetCustomAttributes<PageTestAttribute>()
                             ?? Enumerable.Empty<PageTestAttribute>();

            foreach (var attr in attributes)
            {
                TestResultCollector.Record(
                    TestContext.TestName ?? "",
                    attr.PageUrl,
                    TestContext.CurrentTestOutcome,
                    failed ? tracePath : null,
                    failed ? videoPath : null);
            }
        }

        protected async Task<AccessibilityCheckResult> CheckAccessibility()
        {
            var axeResult  = await Page.RunAxe();
            var violations = axeResult.Violations
                .Select(v => $"[{v.Impact}] {v.Id}: {v.Description}")
                .ToList();

            var result = new AccessibilityCheckResult(Page.Url, violations);
            AccessibilityResultCollector.Record(result);
            return result;
        }
    }
}
