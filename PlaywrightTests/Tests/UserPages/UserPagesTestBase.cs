using PlaywrightTests.Setup;

namespace PlaywrightTests.Tests.UserPages
{
    [TestCategory("Member")]
    public abstract class UserPagesTestBase : AuthenticatedPageTest
    {
        protected override string AuthStateFile => TestUsers.Member.AuthStateFile;
    }
}
