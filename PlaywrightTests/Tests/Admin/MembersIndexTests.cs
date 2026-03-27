namespace PlaywrightTests.Tests.Admin
{
    [TestClass]
    public class MembersIndexTests : AdminTestBase
    {
        // FillAsync on type="search" does not reliably fire the input event in Playwright/Chromium.
        // We set the value and dispatch a real InputEvent directly via JS to guarantee the
        // debounce handler runs. DispatchEventAsync fires a generic Event which is not enough.
        private async Task FillSearch(string term)
        {
            await Page.EvaluateAsync(@"(value) => {
                const el = document.getElementById('memberSearch');
                el.value = value;
                el.dispatchEvent(new InputEvent('input', { bubbles: true }));
            }", term);
        }

        [TestMethod]
        [PageTest("/Admin/Members/Index", "Smoke test - page loads")]
        public async Task Index_Loads()
        {
            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Admin/Members");
            await Expect(Page).ToHaveTitleAsync("Nafnaskrá");
        }

        [TestMethod]
        [PageTest("/Admin/Members/Index", "Search finds member using exact name, accent-folded, and lowercase — row click and Nánar button both navigate to correct details page")]
        public async Task Index_SearchAndNavigate()
        {
            const string memberName = "Bjarni Pétur Friðjónsson";

            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Admin/Members");

            // --- Search with 3 different terms — all must find the member ---
            var searchTerms = new[] { "Bjarni", "Bjarni Pétur", "Bjarni Petur", "bjarni petur fridjonsson" };
            foreach (var term in searchTerms)
            {
                // Clear first so table hides, confirming the next result is fresh
                await FillSearch("");
                await Expect(Page.Locator("#memberTableWrap")).ToBeHiddenAsync();

                await FillSearch(term);
                await Expect(Page.Locator("#memberTableWrap")).ToBeVisibleAsync();
                await Expect(Page.GetByText(memberName, new() { Exact = true })).ToBeVisibleAsync();
            }

            // --- Row click navigates to details ---
            // Click the name cell (first <td>) to trigger gridOnClick, not the Nánar link
            await Page.Locator("table tbody tr")
                .Filter(new() { HasText = memberName })
                .Locator("td").First
                .ClickAsync();
            await Page.WaitForURLAsync("**/Details/Details**");
            await Expect(Page.GetByText(memberName, new() { Exact = true })).ToBeVisibleAsync();

            // --- Nánar button navigates to the same details page ---
            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Admin/Members");
            await FillSearch("Bjarni");
            await Expect(Page.Locator("#memberTableWrap")).ToBeVisibleAsync();
            await Page.Locator("table tbody tr")
                .Filter(new() { HasText = memberName })
                .GetByRole(AriaRole.Link, new() { Name = "Nánar" })
                .ClickAsync();
            await Page.WaitForURLAsync("**/Details/Details**");
            await Expect(Page.GetByText(memberName, new() { Exact = true })).ToBeVisibleAsync();
        }

        [TestMethod]
        [PageTest("/Admin/Members/Index", "Sögufærslur toggle — inactive member hidden without it, visible with it, navigation to details correct")]
        public async Task Index_HistoryToggle_ShowsInactiveMember()
        {
            const string memberName = "Þórmundur Sigurbjarnason";

            // --- Variant 1: search first, then toggle on ---
            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Admin/Members");
            await FillSearch("þórmundur");

            // Without history: member must be absent
            await Expect(Page.GetByText("Enginn félagi fannst.")).ToBeVisibleAsync();
            await Expect(Page.GetByText(memberName, new() { Exact = true })).Not.ToBeVisibleAsync();

            // Toggle Sögufærslur ON — re-fires the search automatically
            await Page.Locator("#historyToggle").ClickAsync();
            await Expect(Page.Locator("#memberTableWrap")).ToBeVisibleAsync();
            await Expect(Page.GetByText(memberName, new() { Exact = true })).ToBeVisibleAsync();

            // Toggle OFF — member disappears again
            await Page.Locator("#historyToggle").ClickAsync();
            await Expect(Page.GetByText("Enginn félagi fannst.")).ToBeVisibleAsync();
            await Expect(Page.GetByText(memberName, new() { Exact = true })).Not.ToBeVisibleAsync();

            // Toggle ON again — member reappears (confirms toggle fires a fresh fetch each time)
            await Page.Locator("#historyToggle").ClickAsync();
            await Expect(Page.GetByText(memberName, new() { Exact = true })).ToBeVisibleAsync();

            // --- Variant 2: toggle on first, then search ---
            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Admin/Members");
            await Page.Locator("#historyToggle").ClickAsync();
            // No search term yet — table stays hidden (length < 3)
            await Expect(Page.Locator("#memberTableWrap")).ToBeHiddenAsync();

            await FillSearch("þórmundur");
            await Expect(Page.Locator("#memberTableWrap")).ToBeVisibleAsync();
            await Expect(Page.GetByText(memberName, new() { Exact = true })).ToBeVisibleAsync();

            // --- Navigate to details and verify the name ---
            await Page.Locator("table tbody tr")
                .Filter(new() { HasText = memberName })
                .GetByRole(AriaRole.Link, new() { Name = "Nánar" })
                .ClickAsync();
            await Page.WaitForURLAsync("**/Details/Details**");
            await Expect(Page.GetByText(memberName, new() { Exact = true })).ToBeVisibleAsync();
        }
    }
}
