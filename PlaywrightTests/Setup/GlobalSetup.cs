using System.Diagnostics;
using System.Net;
using PlaywrightTests.Database;

namespace PlaywrightTests.Setup
{
    [TestClass]
    public static class GlobalSetup
    {
        private static Process? _apiProcess;
        private static Process? _webProcess;
        private static string _solutionRoot = "";
        private static IPlaywright? _playwright;
        private static bool _usingExternalChrome;

        private const string CdpUrl = "http://127.0.0.1:9222";

        // One shared browser for the entire test run.
        // One shared context per role — persists across all tests for that role.
        internal static IBrowser SharedBrowser { get; private set; } = null!;
        internal static readonly Dictionary<string, IBrowserContext> SharedContexts = new();

        [AssemblyInitialize]
        public static async Task Initialize(TestContext context)
        {
            _solutionRoot = GetSolutionRoot();

            if (TestSettings.StartApps)
            {
                _apiProcess = StartProcess(TestSettings.AmplusApiProject);
                _webProcess = StartProcess(TestSettings.CmsWebProject);
            }

            await WaitForHealthy(TestSettings.CmsWebUrl, "CmsWeb");
            await WaitForHealthy(TestSettings.AmplusApiUrl, "AmplusWeb API");
           
            using (var api = new AmplusApi(TestSettings.AmplusApiUrl, TestSettings.AmplusApiKey))
            {
                foreach (var user in TestUsers.All)
                    await api.EnsureUser(user.Username, user.IdNumber, user.Name);
            }

            var headed = Environment.GetEnvironmentVariable("HEADED") == "1";
            var pauseOnFail = Environment.GetEnvironmentVariable("PAUSE_ON_FAIL") == "1";
            var slowMo = float.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_SLOWMO"), out var ms) ? ms : 0;
            var sharedContext = Environment.GetEnvironmentVariable("SHARED_CONTEXT") == "1";

            var cdpAvailable = await IsCdpAvailable();
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "pw-debug.txt"),
            $"HEADED={Environment.GetEnvironmentVariable("HEADED")} headed={headed}\n" +
            $"PAUSE_ON_FAIL={Environment.GetEnvironmentVariable("PAUSE_ON_FAIL")} pauseOnFail={pauseOnFail}\n" +
            $"IsCdpAvailable={cdpAvailable}\n");

            _playwright = await Playwright.CreateAsync();

            // Phase 1: always headless, no slow-mo — log in and save auth state for each role.
            // Fast and invisible regardless of debug settings.
            var loginBrowser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
            foreach (var user in TestUsers.All)
                await LoginAndSaveState(loginBrowser, user);
            await loginBrowser.CloseAsync();

            // Phase 2: launch the real browser with the user's preferred settings.
            // When PAUSE_ON_FAIL is set, connect to a pre-launched external Chrome via CDP.
            // An external Chrome is not a child process of the test runner, so it survives
            // when dotnet test exits. Run start-debug-chrome.cmd before running tests.
            if (headed && pauseOnFail && cdpAvailable)
            {
                SharedBrowser = await _playwright.Chromium.ConnectOverCDPAsync(CdpUrl, new() { SlowMo = slowMo });
                _usingExternalChrome = true;
                Console.WriteLine($"Connected to external Chrome via CDP ({CdpUrl}). Failed tabs will stay open after the run.");
            }
            else
            {
                if (headed && pauseOnFail)
                    Console.WriteLine($"No Chrome found on {CdpUrl}. Run start-debug-chrome.cmd first to keep the browser open after failures. Falling back to managed browser.");

                SharedBrowser = await _playwright.Chromium.LaunchAsync(new() { Headless = !headed, SlowMo = slowMo });
            }

            

            // Optionally create shared contexts from the saved auth state files.
            if (sharedContext)
            {
                foreach (var user in TestUsers.All)
                    await CreateSharedContext(user.AuthStateFile);
            }
        }

        [AssemblyCleanup]
        public static async Task Cleanup()
        {
            foreach (var ctx in SharedContexts.Values)
                await ctx.CloseAsync();
            SharedContexts.Clear();

            if (_usingExternalChrome)
            {
                // Chrome is external — we don't own it, so never close it.
                if (AuthenticatedPageTest.HasFailures)
                    Console.WriteLine("\nFailed tabs are open in the external Chrome. Close it manually when done.");
            }
            else
            {
                if (SharedBrowser != null)
                    await SharedBrowser.CloseAsync();

                _playwright?.Dispose();
            }

            TestResultCollector.DeletePassedVideos();

            var reportPath = Path.Combine(_solutionRoot, "PlaywrightTests", "coverage-report.html");
            CoverageReportGenerator.Generate(_solutionRoot, reportPath);
            Console.WriteLine($"Coverage report written to: {reportPath}");

            KillProcess(_webProcess);
            KillProcess(_apiProcess);
        }

        private static async Task<bool> IsCdpAvailable()
        {
            try
            {
                /*
                 * To run locally for debugging startup issues, start a Chrome with remote debugging enabled:
                 * %PW_DIR% gives you the path to the Playwright package
                 * %PW_DIR%\chromium-1155\chrome-win\chrome.exe --remote-debugging-port=9222 --user-data-dir="%TEMP%\pw-debug-chrome" --no-first-run
                 */
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync($"{CdpUrl}/json/version");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Phase 1: log in using the given browser, save auth state to disk.
        // Always fast and headless — called on a temporary throw-away browser.
        private static async Task LoginAndSaveState(IBrowser browser, TestUsers.TestUser user)
        {
            var loginContext = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            var page = await loginContext.NewPageAsync();

            await page.GotoAsync($"{TestSettings.CmsWebUrl}/Login");
            await page.FillAsync("input[name='Username']", user.Username);
            await page.FillAsync("input[name='password']", user.Password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync($"{TestSettings.CmsWebUrl}/UserPages**");

            await loginContext.StorageStateAsync(new() { Path = user.AuthStateFile });
            await loginContext.CloseAsync();
        }

        // Phase 2: create a shared context on SharedBrowser, loading auth state from disk.
        // SHARED_CONTEXT=1 only — tests reuse these instead of creating their own contexts.
        private static async Task CreateSharedContext(string stateFile)
        {
            SharedContexts[stateFile] = await SharedBrowser.NewContextAsync(new()
            {
                StorageStatePath = stateFile,
                IgnoreHTTPSErrors = true,
                RecordVideoDir = AuthenticatedPageTest.ArtifactDir,
                RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
            });
        }

        private static Process StartProcess(string projectPath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_solutionRoot, projectPath));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{fullPath}\"",
                    WorkingDirectory = _solutionRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            return process;
        }

        private static async Task WaitForHealthy(string baseUrl, string name)
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var client = new HttpClient(handler);
            var deadline = DateTime.UtcNow.AddSeconds(TestSettings.StartupTimeoutSeconds);

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var response = await client.GetAsync(baseUrl);
                    if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
                        return;
                }
                catch { }

                await Task.Delay(TestSettings.HealthCheckIntervalMs);
            }

            throw new Exception($"Timed out waiting for {name} at {baseUrl}");
        }

        private static void KillProcess(Process? process)
        {
            if (process == null || process.HasExited) return;
            process.Kill(entireProcessTree: true);
            process.Dispose();
        }

        private static string GetSolutionRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !dir.GetFiles("*.sln").Any())
                dir = dir.Parent;
            return dir?.FullName ?? throw new Exception("Could not find solution root");
        }
    }
}
