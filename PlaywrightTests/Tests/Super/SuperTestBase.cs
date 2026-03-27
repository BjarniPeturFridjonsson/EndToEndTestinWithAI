using PlaywrightTests.Setup;

namespace PlaywrightTests.Tests.Super
{
    [TestCategory("Super")]
    public abstract class SuperTestBase : AuthenticatedPageTest
    {
        protected override string AuthStateFile => TestUsers.Super.AuthStateFile;
    }
}
