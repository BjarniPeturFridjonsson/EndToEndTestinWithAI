using System.Text;
using System.Text.Json;

namespace PlaywrightTests.Database
{
    /// <summary>
    /// Direct access to the AmplusWeb REST API for test setup.
    /// Used to seed member records that correspond to test users.
    /// </summary>
    public class AmplusApi : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        private static readonly Guid PostcodeGuid = new Guid("0000000000000000000000000000");
        private static readonly Guid CountryGuid = new Guid("000000000000000000000000000000");
        private static readonly Guid _emailContactTypeGuid = new Guid("000000000000000000000000000");

        public AmplusApi(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("X-ApiKey", apiKey);
        }

        /// <summary>
        /// Creates an AmplusWeb member if one with that email does not already exist,
        /// then creates the email contact record for the member.
        /// UserName is set to the email so the login resolves to this member.
        /// </summary>
        public async Task EnsureUser(string email, string idNumber, string name)
        {
            var check = await _http.GetAsync($"{_baseUrl}/api/Members/email/{Uri.EscapeDataString(email)}");
            if (check.IsSuccessStatusCode)
                return;

            var memberGuid = Guid.NewGuid();

            var memberDto = new
            {
                MemberGuid = memberGuid,
                IdNumber = idNumber,
                Name = name,
                Address = "Prófgata 1",
                PostcodeGuid = PostcodeGuid,
                PostcodeId = 10,
                CountryGuid = CountryGuid,
                History = 0,
                Deleted = 0,
                UserName = email,
                ChangeTime = DateTime.Now,
                ChangeUser = "TestSetup",
                ChangeClient = "TestSetup"
            };

            var response = await PostJson("/api/Members", memberDto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create test member '{name}' ({email}): {response.StatusCode} — {error}");
            }

            var contactDto = new
            {
                MemberContactGuid = Guid.NewGuid(),
                MemberGuid = memberGuid,
                ContactTypeGuid = _emailContactTypeGuid,
                ContactTypeId = 0,
                ContactName = email,
                ChangeTime = DateTime.Now,
                ChangeUser = "TestSetup",
                ChangeClient = "TestSetup"
            };

            var contactResponse = await PostJson("/api/Contacts", contactDto);
            if (!contactResponse.IsSuccessStatusCode)
            {
                var error = await contactResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create email contact for '{email}': {contactResponse.StatusCode} — {error}");
            }
        }

        private async Task<HttpResponseMessage> PostJson(string path, object dto)
        {
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _http.PostAsync($"{_baseUrl}{path}", content);
        }

        public void Dispose() => _http.Dispose();
    }
}
