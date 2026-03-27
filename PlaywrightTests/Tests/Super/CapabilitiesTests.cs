namespace PlaywrightTests.Tests.Super
{
    [TestClass]
    public class CapabilitiesTests : SuperTestBase
    {
        [TestMethod]
        [PageTest("/Super/Capabilities/Index", "Smoke test - page loads")]
        public async Task Index_Loads()
        {
            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities");
            await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(".+"));
        }

        [TestMethod]
        [PageTest("/Super/Capabilities/Create", "Full lifecycle - create, add 3 members, verify delete is blocked, remove members, delete")]
        public async Task Capabilities_FullLifecycle()
        {
            const string capabilityName = "TestCap-PLAYWRIGHT";
            string capabilityGuid = null;

            try
            {
                // --- Create ---
                await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/Create");
                await Page.Locator("[name='CapabilityName']").FillAsync(capabilityName);
                await Page.GetByRole(AriaRole.Button, new() { Name = "Staðfesta" }).ClickAsync();
                await Page.WaitForURLAsync("**/Details**");

                // Extract capability GUID from the redirect URL: .../Details?id={guid}
                capabilityGuid = Page.Url.Split("?id=", 2)[1].Split('&')[0];

                // --- Verify in Index ---
                await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities");
                await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = capabilityName, Exact = true })).ToBeVisibleAsync();

                // --- Details page shows the capability name ---
                await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/Details?id={capabilityGuid}");
                await Expect(Page.GetByLabel("Heiti réttinda:")).ToHaveValueAsync(capabilityName);

                // --- Add 3 members via EditRow (new) ---
                // Each member is found via DoSearchableDrop (type in search box, pick from dropdown)
                // UI flag: JS console error occurs on EditRow (existing) — see known issue in EditRow.cshtml
                var memberSearchTerms = new[] { TestUsers.Admin.Name, TestUsers.Member.Name, TestUsers.Training.Name};
                foreach (var searchTerm in memberSearchTerms)
                {
                    await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/EditRow?capabilityGuid={capabilityGuid}");
                    await Page.Locator("#MemberName").FillAsync(searchTerm);
                    await Page.Locator("#sfiSearchableResultList li").First.WaitForAsync();
                    await Page.Locator("#sfiSearchableResultList li").First.ClickAsync();
                    await Page.GetByRole(AriaRole.Button, new() { Name = "Vista" }).ClickAsync();
                    await Page.WaitForURLAsync("**/CapabilityRows**");
                }

                // After last add we are already on CapabilityRows — verify all 3 rows are there
                var rowCount = await Page.Locator("table tbody tr").CountAsync();
                Assert.AreEqual(3, rowCount, "Expected 3 member rows in CapabilityRows table");

                // --- Attempt delete with members present - must be blocked ---
                await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/Delete?id={capabilityGuid}");
                await Expect(Page.GetByText("Ekki hægt að fella þessi réttindi.")).ToBeVisibleAsync();
                await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Fella" })).Not.ToBeVisibleAsync();

                // --- Remove all members one by one ---
                await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/CapabilityRows?id={capabilityGuid}");
                while (!await Page.GetByText("Engar færslur fundust.").IsVisibleAsync())
                {
                    await Page.Locator("table tbody tr").First.ClickAsync();
                    await Page.WaitForURLAsync("**/EditRow**");
                    await Page.GetByRole(AriaRole.Button, new() { Name = "Eyða" }).ClickAsync();
                    await Page.GetByRole(AriaRole.Button, new() { Name = "Staðfesta" }).ClickAsync();
                    await Page.WaitForURLAsync("**/CapabilityRows**");
                }

                // --- Delete capability (now empty) ---
                await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/Delete?id={capabilityGuid}");
                await Page.GetByRole(AriaRole.Button, new() { Name = "Fella" }).ClickAsync();
                await Expect(Page).ToHaveTitleAsync("Réttindi og hæfni");
                await Expect(Page.GetByText(capabilityName)).Not.ToBeVisibleAsync();

                capabilityGuid = null; // deleted — nothing to clean up
            }
            finally
            {
                if (capabilityGuid != null)
                {
                    // Remove any remaining rows
                    await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/CapabilityRows?id={capabilityGuid}");
                    while (!await Page.GetByText("Engar færslur fundust.").IsVisibleAsync())
                    {
                        await Page.Locator("table tbody tr").First.ClickAsync();
                        await Page.WaitForURLAsync("**/EditRow**");
                        await Page.GetByRole(AriaRole.Button, new() { Name = "Eyða" }).ClickAsync();
                        await Page.GetByRole(AriaRole.Button, new() { Name = "Staðfesta" }).ClickAsync();
                        await Page.WaitForURLAsync("**/CapabilityRows**");
                    }
                    // Delete the capability
                    await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Super/Capabilities/Delete?id={capabilityGuid}");
                    await Page.GetByRole(AriaRole.Button, new() { Name = "Fella" }).ClickAsync();
                }
            }
        }
    }
}
