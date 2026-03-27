using System.Collections.Concurrent;

namespace PlaywrightTests.Reporting
{
    public class PageTestResult
    {
        public string  TestName   { get; init; } = "";
        public string  PageUrl    { get; init; } = "";
        public string  Outcome    { get; init; } = "";
        public string? TracePath  { get; init; }
        public string? VideoPath  { get; init; }
        public bool    Passed     => Outcome == "Passed";
        public bool    Failed     => Outcome == "Failed";
    }

    public static class TestResultCollector
    {
        private static readonly ConcurrentBag<PageTestResult>                          _results = new();
        private static readonly ConcurrentBag<(string, UnitTestOutcome, string?)>      _allRuns = new();

        /// <summary>Records a result tied to a specific page URL (for coverage report).</summary>
        public static void Record(string testName, string pageUrl, UnitTestOutcome outcome, string? tracePath, string? videoPath)
        {
            _results.Add(new PageTestResult
            {
                TestName  = testName,
                PageUrl   = pageUrl,
                Outcome   = outcome.ToString(),
                TracePath = tracePath,
                VideoPath = videoPath
            });
        }

        /// <summary>Records every test run so we can clean up artifacts afterwards.</summary>
        public static void RecordRun(string testName, UnitTestOutcome outcome, string? videoPath)
        {
            _allRuns.Add((testName, outcome, videoPath));
        }

        public static IReadOnlyList<PageTestResult> GetAll() => _results.ToList();

        /// <summary>Deletes video files for tests that passed — keeps artifacts folder lean.</summary>
        public static void DeletePassedVideos()
        {
            foreach (var (_, outcome, videoPath) in _allRuns)
            {
                if (outcome == UnitTestOutcome.Passed && videoPath != null && File.Exists(videoPath))
                    File.Delete(videoPath);
            }
        }
    }

    // -------------------------------------------------------------------------

    public class AccessibilityCheckResult
    {
        public string       PageUrl    { get; }
        public List<string> Violations { get; }
        public bool         HasIssues  => Violations.Count > 0;

        public AccessibilityCheckResult(string pageUrl, List<string> violations)
        {
            PageUrl    = pageUrl;
            Violations = violations;
        }
    }

    public static class AccessibilityResultCollector
    {
        private static readonly ConcurrentBag<AccessibilityCheckResult> _results = new();

        public static void Record(AccessibilityCheckResult result) => _results.Add(result);

        public static IReadOnlyList<AccessibilityCheckResult> GetAll() => _results.ToList();
    }
}
