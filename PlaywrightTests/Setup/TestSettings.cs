using System.Text.Json;

namespace PlaywrightTests.Setup
{
    public static class TestSettings
    {
        private static readonly Settings _settings = Load();

        public static string CmsWebUrl        => _settings.CmsWebUrl;
        public static string AmplusApiUrl     => _settings.AmplusApiUrl;
        public static string AmplusApiKey     => _settings.AmplusApiKey;
        public static string CmsWebProject    => _settings.CmsWebProject;
        public static string AmplusApiProject => _settings.AmplusApiProject;
        public static bool   StartApps        => _settings.StartApps;

        public const int StartupTimeoutSeconds = 60;
        public const int HealthCheckIntervalMs = 1000;

        private static Settings Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
            if (!File.Exists(path))
                throw new FileNotFoundException($"testsettings.json not found at {path}. Copy testsettings.example.json and fill in your values.");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Failed to deserialize testsettings.json");
        }

        private class Settings
        {
            public string CmsWebUrl        { get; set; } = "";
            public string AmplusApiUrl     { get; set; } = "";
            public string AmplusApiKey     { get; set; } = "";
            public string CmsWebProject    { get; set; } = "";
            public string AmplusApiProject { get; set; } = "";
            public bool   StartApps        { get; set; } = true;
        }

    }
}
