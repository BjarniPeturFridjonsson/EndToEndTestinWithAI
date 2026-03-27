using System.Reflection;
using System.Text;

namespace PlaywrightTests.Reporting
{
    public class PageCoverage
    {
        public DiscoveredPage            Page          { get; init; } = new();
        public List<CoveredTest>         Tests         { get; init; } = new();
        public List<string>              A11yViolations { get; init; } = new();
        public bool                      IsCovered     => Tests.Count > 0;
        public int                       Passed        => Tests.Count(t => t.Result?.Passed == true);
        public int                       Failed        => Tests.Count(t => t.Result?.Failed == true);
        public int                       NotRun        => Tests.Count(t => t.Result == null);
        public bool                      HasA11yIssues => A11yViolations.Count > 0;
    }

    public class CoveredTest
    {
        public string          TestName    { get; init; } = "";
        public string          Description { get; init; } = "";
        public PageTestResult? Result      { get; set; }
    }

    public static class CoverageReportGenerator
    {
        public static void Generate(string solutionRoot, string outputPath)
        {
            var pages      = PageDiscovery.Discover(solutionRoot);
            var tests      = DiscoverTestMethods();
            var results    = TestResultCollector.GetAll();
            var a11y       = AccessibilityResultCollector.GetAll();

            var coverage   = BuildCoverage(pages, tests, results, a11y);

            File.WriteAllText(outputPath, BuildHtml(coverage), Encoding.UTF8);
        }

        private static List<(MethodInfo Method, string PageUrl, string Description)> DiscoverTestMethods()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .SelectMany(m => m.GetCustomAttributes<PageTestAttribute>()
                    .Select(a => (Method: m, PageUrl: a.PageUrl, Description: a.Description)))
                .ToList();
        }

        private static List<PageCoverage> BuildCoverage(
            List<DiscoveredPage>                                        pages,
            List<(MethodInfo Method, string PageUrl, string Description)> testMethods,
            IReadOnlyList<PageTestResult>                               results,
            IReadOnlyList<AccessibilityCheckResult>                     a11yResults)
        {
            return pages.Select(page =>
            {
                var tests = testMethods
                    .Where(t => t.PageUrl.Equals(page.Url, StringComparison.OrdinalIgnoreCase))
                    .Select(t => new CoveredTest
                    {
                        TestName    = t.Method.Name,
                        Description = t.Description,
                        Result      = results.FirstOrDefault(r =>
                            r.TestName == t.Method.Name &&
                            r.PageUrl.Equals(page.Url, StringComparison.OrdinalIgnoreCase))
                    })
                    .ToList();

                var violations = a11yResults
                    .Where(r => r.PageUrl.Contains(page.Url, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(r => r.Violations)
                    .Distinct()
                    .ToList();

                return new PageCoverage { Page = page, Tests = tests, A11yViolations = violations };
            }).ToList();
        }

        private static string BuildHtml(List<PageCoverage> coverage)
        {
            var total        = coverage.Count;
            var covered      = coverage.Count(c => c.IsCovered);
            var failing      = coverage.Count(c => c.IsCovered && c.Failed > 0);
            var missing      = total - covered;
            var pct          = total == 0 ? 0 : (covered * 100 / total);
            var a11yChecked  = coverage.Count(c => c.A11yViolations.Count > 0 || c.Tests.Any(t => t.Result != null));
            var a11yClean    = coverage.Count(c => c.IsCovered && !c.HasA11yIssues && c.Tests.Any(t => t.Result != null));
            var a11yFailing  = coverage.Count(c => c.HasA11yIssues);

            var sb = new StringBuilder();
            sb.AppendLine("""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <title>Playwright Test Coverage</title>
                  <style>
                    body  { font-family: sans-serif; margin: 2rem; background: #f9f9f9; color: #222; }
                    h1    { margin-bottom: 0.25rem; }
                    h2    { margin: 2rem 0 0.75rem; font-size: 1.1rem; color: #444; }
                    .ts   { color: #666; font-size: 0.9rem; margin-bottom: 1.5rem; }
                    .summary { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; flex-wrap: wrap; }
                    .stat    { background: #fff; border: 1px solid #ddd; border-radius: 6px; padding: 1rem 1.5rem; min-width: 120px; text-align: center; }
                    .stat .n { font-size: 2rem; font-weight: bold; }
                    .stat .l { font-size: 0.85rem; color: #666; }
                    .green  { color: #2a9d2a; }
                    .red    { color: #c0392b; }
                    .orange { color: #e67e22; }
                    .filters { margin-bottom: 1rem; display: flex; gap: 0.5rem; flex-wrap: wrap; }
                    .filters button { padding: 0.4rem 1rem; border: 1px solid #ccc; border-radius: 4px; cursor: pointer; background: #fff; font-size: 0.9rem; }
                    .filters button.active { background: #333; color: #fff; border-color: #333; }
                    .area-stats { display: flex; gap: 1rem; margin-bottom: 0.75rem; flex-wrap: wrap; }
                    .area-stats .stat { padding: 0.5rem 1rem; min-width: 80px; }
                    .area-stats .stat .n { font-size: 1.4rem; }
                    table  { width: 100%; border-collapse: collapse; background: #fff; border-radius: 6px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); margin-bottom: 2rem; }
                    th     { background: #333; color: #fff; text-align: left; padding: 0.6rem 1rem; font-size: 0.85rem; }
                    td     { padding: 0.55rem 1rem; border-bottom: 1px solid #eee; font-size: 0.9rem; vertical-align: top; }
                    tr:last-child td { border-bottom: none; }
                    tr.covered-pass  { background: #f0fff0; }
                    tr.covered-fail  { background: #fff5f5; }
                    tr.not-covered   { background: #fffbf0; }
                    tr.a11y-fail     { background: #fff0f8; }
                    .badge           { display: inline-block; padding: 0.15rem 0.55rem; border-radius: 3px; font-size: 0.78rem; font-weight: bold; }
                    .badge-pass      { background: #d4edda; color: #155724; }
                    .badge-fail      { background: #f8d7da; color: #721c24; }
                    .badge-norun     { background: #e2e3e5; color: #383d41; }
                    .badge-none      { background: #fff3cd; color: #856404; }
                    .badge-a11y-ok   { background: #d4edda; color: #155724; }
                    .badge-a11y-fail { background: #f8d7da; color: #721c24; }
                    .badge-a11y-skip { background: #e2e3e5; color: #555; }
                    .test-list  { margin: 0.25rem 0 0 0; padding: 0; list-style: none; }
                    .test-list li { font-size: 0.82rem; color: #555; margin-bottom: 0.25rem; }
                    .a11y-list  { margin: 0.25rem 0 0 0; padding: 0 0 0 1rem; font-size: 0.8rem; color: #900; }
                    .artifacts  { margin-top: 0.3rem; }
                    .artifacts a { font-size: 0.78rem; color: #0066cc; margin-right: 0.75rem; }
                    .url        { font-family: monospace; font-size: 0.88rem; }
                    .area-tag   { font-size: 0.75rem; color: #888; }
                  </style>
                </head>
                <body>
                """);

            sb.AppendLine("<h1>Playwright Test Coverage</h1>");
            sb.AppendLine($"<div class='ts'>Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>");

            // ---- Summary stats ----
            sb.AppendLine("<h2>Coverage</h2><div class='summary'>");
            sb.AppendLine(Stat(total.ToString(),   "Total Pages",   ""));
            sb.AppendLine(Stat(covered.ToString(),  "Covered",       "green"));
            sb.AppendLine(Stat(missing.ToString(),  "Not Covered",   missing  > 0 ? "orange" : ""));
            sb.AppendLine(Stat(failing.ToString(),  "Test Failures", failing  > 0 ? "red"    : ""));
            sb.AppendLine(Stat($"{pct}%",           "Coverage",      pct >= 80 ? "green" : pct >= 50 ? "orange" : "red"));
            sb.AppendLine("</div>");

            sb.AppendLine("<h2>Accessibility (axe-core)</h2><div class='summary'>");
            sb.AppendLine(Stat(a11yChecked.ToString(), "Pages Audited",  ""));
            sb.AppendLine(Stat(a11yClean.ToString(),   "Clean",          a11yClean > 0 ? "green" : ""));
            sb.AppendLine(Stat(a11yFailing.ToString(), "Violations",     a11yFailing > 0 ? "red" : "green"));
            sb.AppendLine("</div>");

            // ---- Filter buttons ----
            sb.AppendLine("""
                <div class='filters'>
                  <button class='active' onclick="filter('all', this)">All</button>
                  <button onclick="filter('covered', this)">Covered</button>
                  <button onclick="filter('not-covered', this)">Not Covered</button>
                  <button onclick="filter('failing', this)">Failing</button>
                  <button onclick="filter('a11y-fail', this)">A11y Issues</button>
                </div>
                """);

            // ---- Tables grouped by area ----
            var groups = coverage
                .GroupBy(c => c.Page.Area)
                .OrderBy(g => AreaSortOrder(g.Key));

            foreach (var group in groups)
            {
                var area        = group.Key;
                var pages       = group.ToList();
                var gTotal      = pages.Count;
                var gCovered    = pages.Count(c => c.IsCovered);
                var gFailing    = pages.Count(c => c.Failed > 0);
                var gA11y       = pages.Count(c => c.HasA11yIssues);
                var gPct        = gTotal == 0 ? 0 : (gCovered * 100 / gTotal);
                var category    = AreaToCategory(area);

                sb.AppendLine($"<h2 id='area-{area}'>{area} <span style='font-size:0.8rem;color:#888;font-weight:normal'>({category})</span></h2>");
                sb.AppendLine("<div class='area-stats'>");
                sb.AppendLine(Stat(gTotal.ToString(),   "Pages",    ""));
                sb.AppendLine(Stat(gCovered.ToString(), "Covered",  gCovered == gTotal ? "green" : "orange"));
                sb.AppendLine(Stat(gFailing.ToString(), "Failing",  gFailing > 0 ? "red" : ""));
                sb.AppendLine(Stat(gA11y.ToString(),    "A11y",     gA11y > 0 ? "red" : ""));
                sb.AppendLine(Stat($"{gPct}%",          "Coverage", gPct >= 80 ? "green" : gPct >= 50 ? "orange" : "red"));
                sb.AppendLine("</div>");

                sb.AppendLine($"<table class='area-table' data-area='{area}'>");
                sb.AppendLine("<thead><tr><th>Page</th><th>Tests</th><th>Accessibility</th></tr></thead><tbody>");

                foreach (var c in pages)
                {
                    var states = new List<string>();
                    if (!c.IsCovered)       states.Add("not-covered");
                    if (c.Failed > 0)       states.Add("failing");
                    if (c.HasA11yIssues)    states.Add("a11y-fail");
                    if (states.Count == 0)  states.Add("covered");

                    var rowClass = c.HasA11yIssues ? "a11y-fail"
                                 : c.Failed > 0    ? "covered-fail"
                                 : c.IsCovered     ? "covered-pass"
                                 :                   "not-covered";

                    sb.AppendLine($"<tr class='{rowClass}' data-state='{string.Join(" ", states)}' data-area='{area}'>");
                    sb.AppendLine($"  <td><span class='url'>{c.Page.Url}</span></td>");

                    // Tests column
                    sb.AppendLine("  <td>");
                    if (!c.IsCovered)
                    {
                        sb.AppendLine("    <span class='badge badge-none'>No tests</span>");
                    }
                    else
                    {
                        sb.AppendLine("    <ul class='test-list'>");
                        foreach (var t in c.Tests)
                        {
                            var outcome = t.Result == null    ? "<span class='badge badge-norun'>not run</span>"
                                        : t.Result.Passed     ? "<span class='badge badge-pass'>✓</span>"
                                        :                       "<span class='badge badge-fail'>✗</span>";

                            var desc = string.IsNullOrEmpty(t.Description) ? t.TestName : $"{t.TestName} — {t.Description}";
                            sb.AppendLine($"      <li>{outcome} {desc}");

                            if (t.Result?.TracePath != null || t.Result?.VideoPath != null)
                            {
                                sb.AppendLine("        <div class='artifacts'>");
                                if (t.Result.TracePath != null)
                                    sb.AppendLine($"          <a href='https://trace.playwright.dev/' title='{t.Result.TracePath}'>📋 Trace</a>");
                                if (t.Result.VideoPath != null)
                                    sb.AppendLine($"          <a href='file:///{t.Result.VideoPath.Replace('\\', '/')}'>🎬 Video</a>");
                                sb.AppendLine("        </div>");
                            }

                            sb.AppendLine("      </li>");
                        }
                        sb.AppendLine("    </ul>");
                    }
                    sb.AppendLine("  </td>");

                    // Accessibility column
                    sb.AppendLine("  <td>");
                    if (!c.IsCovered)
                        sb.AppendLine("    <span class='badge badge-a11y-skip'>not audited</span>");
                    else if (!c.HasA11yIssues)
                        sb.AppendLine("    <span class='badge badge-a11y-ok'>✓ clean</span>");
                    else
                    {
                        sb.AppendLine($"    <span class='badge badge-a11y-fail'>{c.A11yViolations.Count} violation{(c.A11yViolations.Count == 1 ? "" : "s")}</span>");
                        sb.AppendLine("    <ul class='a11y-list'>");
                        foreach (var v in c.A11yViolations)
                            sb.AppendLine($"      <li>{v}</li>");
                        sb.AppendLine("    </ul>");
                    }
                    sb.AppendLine("  </td>");

                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("""
                <script>
                  function filter(state, btn) {
                    document.querySelectorAll('.filters button').forEach(b => b.classList.remove('active'));
                    btn.classList.add('active');
                    document.querySelectorAll('.area-table tbody tr').forEach(row => {
                      const states = row.dataset.state || '';
                      row.style.display = (state === 'all' || states.includes(state)) ? '' : 'none';
                    });
                    // Hide area sections that have no visible rows
                    document.querySelectorAll('.area-table').forEach(table => {
                      const visible = table.querySelectorAll('tbody tr:not([style*="none"])').length;
                      const section = table.previousElementSibling?.previousElementSibling;
                      if (section && section.tagName === 'H2') section.style.display = visible ? '' : 'none';
                      table.previousElementSibling.style.display = visible ? '' : 'none';
                      table.style.display = visible ? '' : 'none';
                    });
                  }
                </script>
                </body></html>
                """);

            return sb.ToString();
        }

        private static string Stat(string number, string label, string colorClass) =>
            $"<div class='stat'><div class='n {colorClass}'>{number}</div><div class='l'>{label}</div></div>";

        private static int AreaSortOrder(string area) => area switch
        {
            "Admin"    => 1,
            "Training" => 2,
            "Super"    => 3,
            "UserPages"    => 4,
            _              => 5
        };

        private static string AreaToCategory(string area) => area switch
        {
            "Admin"    => "TestCategory=Admin",
            "Training" => "TestCategory=Training",
            "Super"    => "TestCategory=Super",
            "UserPages"    => "TestCategory=Member",
            _              => "no auth required"
        };
    }
}
