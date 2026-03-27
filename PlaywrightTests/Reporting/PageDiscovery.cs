namespace PlaywrightTests.Reporting
{
    public class DiscoveredPage
    {
        public string Url        { get; init; } = "";
        public string FilePath   { get; init; } = "";
        public string Area       { get; init; } = ""; // Super, Admin, Training, UserPages, Other
    }

    public static class PageDiscovery
    {
        private static readonly string[] SkipFolders =
            ["Shared", "DisplayTemplates", "Components", "ViewComponents", "_Fakes"];

        private static readonly string[] SkipFiles =
            ["_Layout", "_ViewStart", "_ViewImports", "NoData", "Error"];

        public static List<DiscoveredPage> Discover(string solutionRoot)
        {
            var pages = new List<DiscoveredPage>();

            pages.AddRange(ScanFolder(solutionRoot, Path.Combine(solutionRoot, "BlueSky", "Pages")));
            pages.AddRange(ScanFolder(solutionRoot, Path.Combine(solutionRoot, "CmsWeb", "Pages")));

            return pages.OrderBy(p => p.Area).ThenBy(p => p.Url).ToList();
        }

        private static IEnumerable<DiscoveredPage> ScanFolder(string solutionRoot, string pagesRoot)
        {
            if (!Directory.Exists(pagesRoot))
                yield break;

            foreach (var file in Directory.GetFiles(pagesRoot, "*.cshtml", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                if (SkipFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (fileName.StartsWith("_"))
                    continue;

                var relativePath = Path.GetRelativePath(pagesRoot, file);
                var parts        = relativePath.Split(Path.DirectorySeparatorChar);

                if (parts.Any(p => SkipFolders.Contains(p, StringComparer.OrdinalIgnoreCase)))
                    continue;

                if (!HasPageDirective(file))
                    continue;

                var url  = "/" + string.Join("/", parts).Replace(".cshtml", "").Replace('\\', '/');
                var area = parts.Length > 1 ? parts[0] : "Other";

                yield return new DiscoveredPage { Url = url, FilePath = file, Area = area };
            }
        }

        private static bool HasPageDirective(string filePath)
        {
            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                return trimmed.StartsWith("@page");
            }
            return false;
        }
    }
}
