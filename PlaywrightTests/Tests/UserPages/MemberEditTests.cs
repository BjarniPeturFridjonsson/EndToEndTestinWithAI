namespace PlaywrightTests.Tests.UserPages
{
    [TestClass]
    public class MemberEditTests : UserPagesTestBase
    {
        [TestMethod]
        [PageTest("/UserPages/MemberEdit", "Smoke test - page loads and is accessible")]
        public async Task MemberEdit_Loads()
        {
            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/UserPages/MemberEdit");
            await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(".+"));
            await CheckAccessibility();
        }

        [TestMethod]
        [PageTest("/UserPages/MemberEdit", "Edit member details - changes persist and display on MembersDetails")]
        public async Task MemberEdit_SavesChanges_VerifiedOnDetailsPage()
        {
            await Page.GotoAsync($"{TestSettings.CmsWebUrl}/UserPages/MemberEdit");

            // Capture original values so we can revert after the test
            var originalName     = await Page.Locator("[name='Cdta.Name']").InputValueAsync();
            var originalAddr1    = await Page.Locator("[name='Cdta.Address']").InputValueAsync();
            var originalAddr2    = await Page.Locator("[name='Cdta.Address1']").InputValueAsync();
            var originalPhone0   = await Page.Locator("[name='phoneNo0']").InputValueAsync();
            var originalPhone1   = await Page.Locator("[name='phoneNo1']").InputValueAsync();
            var originalPhone2   = await Page.Locator("[name='phoneNo2']").InputValueAsync();
            var originalShort    = await Page.Locator("[name='Cdta.NameShort']").InputValueAsync();
            var originalInitials = await Page.Locator("[name='Cdta.NameInitials']").InputValueAsync();
            var originalContact  = await Page.Locator("[name='Cdta.Remarks']").InputValueAsync();

            try
            {
                // Fill in changed values
                await Page.Locator("[name='Cdta.Name']").FillAsync(originalName + " edited");
                await Page.Locator("[name='Cdta.Address']").FillAsync("Edited Street 42");
                await Page.Locator("[name='Cdta.Address1']").FillAsync("Edited Line 2");
                await Page.Locator("[name='phoneNo0']").FillAsync("554-0001");
                await Page.Locator("[name='phoneNo1']").FillAsync("554-0002");
                await Page.Locator("[name='phoneNo2']").FillAsync("554-0003");
                await Page.Locator("[name='Cdta.NameShort']").FillAsync("EditedShort");
                await Page.Locator("[name='Cdta.NameInitials']").FillAsync("EditMk");
                await Page.Locator("[name='Cdta.Remarks']").FillAsync("Edited Contact");

                // Save (anchor has no href so GetByRole(Link) won't match — use GetByText)
                await Page.GetByText("Staðfesta").ClickAsync();
                await Page.WaitForURLAsync("**/MembersDetails**");

                // Verify changes are visible on the details page (which reads from the API)
                await Expect(Page.Locator("strong").First).ToContainTextAsync(originalName + " edited");
                await Expect(Page.GetByText("Edited Street 42")).ToBeVisibleAsync();
                await Expect(Page.GetByText("Edited Line 2")).ToBeVisibleAsync();
                await Expect(Page.GetByText("554-0001")).ToBeVisibleAsync();
                await Expect(Page.GetByText("EditedShort")).ToBeVisibleAsync();
                await Expect(Page.Locator("dt").Filter(new() { HasText = "Upphafsstafir" })
                    .Locator("xpath=following-sibling::dd[1]")).ToContainTextAsync("EditMk");
                await Expect(Page.GetByText("Edited Contact")).ToBeVisibleAsync();
            }
            finally
            {
                // Always revert — test data must not persist between runs
                await Page.GotoAsync($"{TestSettings.CmsWebUrl}/UserPages/MemberEdit");

                await Page.Locator("[name='Cdta.Name']").FillAsync(originalName);
                await Page.Locator("[name='Cdta.Address']").FillAsync(originalAddr1);
                await Page.Locator("[name='Cdta.Address1']").FillAsync(originalAddr2);
                await Page.Locator("[name='phoneNo0']").FillAsync(originalPhone0);
                await Page.Locator("[name='phoneNo1']").FillAsync(originalPhone1);
                await Page.Locator("[name='phoneNo2']").FillAsync(originalPhone2);
                await Page.Locator("[name='Cdta.NameShort']").FillAsync(originalShort);
                await Page.Locator("[name='Cdta.NameInitials']").FillAsync(originalInitials);
                await Page.Locator("[name='Cdta.Remarks']").FillAsync(originalContact);
                await Page.GetByText("Staðfesta").ClickAsync();
                await Page.WaitForURLAsync("**/MembersDetails**");
            }
        }
    }
}
