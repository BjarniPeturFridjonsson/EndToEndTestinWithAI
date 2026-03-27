using System.Text.Json;

namespace PlaywrightTests.Setup
{
    /// <summary>
    /// Central access point for all test users. Loaded directly from testsettings.json.
    /// Each user bundles their credentials with their auth state file path.
    /// Use in tests, GlobalSetup, and anywhere else user data is needed.
    /// </summary>
    public static class TestUsers
    {
        public class TestUser
        {
            public string Username      { get; init; } = "";
            public string Password      { get; init; } = "";
            public string Role          { get; init; } = "";
            public string Name          { get; init; } = "";
            public string IdNumber      { get; init; } = "";
            public string AuthStateFile { get; init; } = "";
        }

        public static readonly TestUser Super;
        public static readonly TestUser Admin;
        public static readonly TestUser Training;
        public static readonly TestUser Member;

        public static readonly IReadOnlyList<TestUser> All;

        static TestUsers()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
            if (!File.Exists(path))
                throw new FileNotFoundException($"testsettings.json not found at {path}.");

            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            var users = doc.RootElement.GetProperty("Users");

            Super    = Load(users, "Super",    "authstate-Super.json");
            Admin    = Load(users, "Admin",    "authstate-Admin.json");
            Training = Load(users, "Training", "authstate-Training.json");
            Member       = Load(users, "Member",       "authstate-member.json");

            All = [Super, Admin, Training, Member];
        }

        private static TestUser Load(JsonElement users, string key, string authStateFile) => new()
        {
            Username      = users.GetProperty(key).GetProperty("Username").GetString()  ?? "",
            Password      = users.GetProperty(key).GetProperty("Password").GetString()  ?? "",
            Role          = users.GetProperty(key).GetProperty("Role").GetString()      ?? "",
            Name          = users.GetProperty(key).GetProperty("Name").GetString()      ?? "",
            IdNumber      = users.GetProperty(key).GetProperty("IdNumber").GetString()  ?? "",
            AuthStateFile = authStateFile
        };
    }
}
