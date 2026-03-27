namespace PlaywrightTests.Reporting
{
    /// <summary>
    /// Marks a test method as covering a specific page URL.
    /// Used by the coverage report to match tests against discovered pages.
    /// A test method can have multiple [PageTest] attributes if it covers multiple pages.
    /// Example: [PageTest("/Super/Capabilities/Index", "Lists all capabilities")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PageTestAttribute : Attribute
    {
        public string PageUrl    { get; }
        public string Description { get; }

        public PageTestAttribute(string pageUrl, string description = "")
        {
            PageUrl     = pageUrl;
            Description = description;
        }
    }
}
