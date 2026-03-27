using PlaywrightTests.Setup;

namespace PlaywrightTests.Tests.Training
{
    [TestCategory("Training")]
    public abstract class TrainingTestBase : AuthenticatedPageTest
    {
        protected override string AuthStateFile => TestUsers.Training.AuthStateFile;
    }
}
