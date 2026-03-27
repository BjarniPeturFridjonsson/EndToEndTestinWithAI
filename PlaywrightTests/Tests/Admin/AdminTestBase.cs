using PlaywrightTests.Setup;

namespace PlaywrightTests.Tests.Admin
{
    [TestCategory("Admin")]
    public abstract class AdminTestBase : AuthenticatedPageTest
    {
        protected override string AuthStateFile => TestUsers.Admin.AuthStateFile;
    }
}
