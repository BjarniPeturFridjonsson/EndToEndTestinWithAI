# Playwright Test Plan

## Test Structure

### Auth States
Four auth states set up once in `GlobalSetup.AssemblyInitialize`, saved to JSON files:
- `authstate-Super.json`    → SuperRole
- `authstate-Admin.json`    → AdminRole
- `authstate-Training.json` → TrainingRole, SuperRole
- `authstate-member.json`       → authenticated member (no club role)

Test users are in `testsettings.json` (git-ignored). See `testsettings.example.json` for structure.

### Base Classes (one per area)

| Base class              | Auth state   | Category       | Location                   |
|-------------------------|--------------|----------------|----------------------------|
| `SuperTestBase`     | Super    | `Super`    | `Tests/Super/`         |
| `AdminTestBase`     | Admin    | `Admin`    | `Tests/Admin/`         |
| `TrainingTestBase`  | Training | `Training` | `Tests/Training/`      |
| `UserPagesTestBase`     | Member       | `Member`       | `Tests/UserPages/`         |

### Running Tests

```bash
# Single role
dotnet test PlaywrightTests/PlaywrightTests.csproj --filter "TestCategory=Admin"
dotnet test PlaywrightTests/PlaywrightTests.csproj --filter "TestCategory=Training"
dotnet test PlaywrightTests/PlaywrightTests.csproj --filter "TestCategory=Member"
dotnet test PlaywrightTests/PlaywrightTests.csproj --filter "TestCategory=Super"
dotnet test PlaywrightTests/PlaywrightTests.csproj --filter "TestCategory=Authorization"
dotnet test PlaywrightTests/PlaywrightTests.csproj --filter "TestCategory=E2E"

# Full parallel pre-release run (all roles simultaneously)
dotnet test PlaywrightTests/PlaywrightTests.csproj --settings PlaywrightTests/parallel.runsettings
```

`parallel.runsettings`: `Workers=4, Scope=ClassLevel` — classes run in parallel,
tests within a class run sequentially.

---

## Infrastructure (all built)

### Tracing
Tracing starts automatically in `AuthenticatedPageTest.[TestInitialize]`.
On failure: trace saved to `artifacts/{TestName}.trace.zip`.
On pass: trace discarded (no file written).
Open traces at https://trace.playwright.dev/ (drag and drop the zip).

### Video recording
Video records for every test into `artifacts/`.
On pass: video file deleted in `GlobalSetup.Cleanup()`.
On failure: video path stored in the test result, linked from coverage report.

### Accessibility auditing (axe-core)
Call `await CheckAccessibility()` in any test after navigating to a page.
Results are collected automatically and shown in the coverage report.
Uses `Deque.AxeCore.Playwright` / WCAG 2.1 rules by default.

```csharp
[TestMethod]
[PageTest("/Admin/Members", "Members index loads and passes accessibility audit")]
public async Task Index_LoadsAndPassesA11y()
{
    await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Admin/Members");
    await Expect(Page).ToHaveTitleAsync(new Regex(".+"));
    var a11y = await CheckAccessibility();
    Assert.AreEqual(0, a11y.Violations.Count, string.Join("\n", a11y.Violations));
}
```

### API interception
`ApiInterceptor` captures outbound HTTP requests without blocking them.
Use in tests that need to verify which API calls a page makes.

```csharp
await using var interceptor = await ApiInterceptor.StartAsync(Page, "**/api/flights/**");
await Page.ClickAsync("button#save");
Assert.IsTrue(interceptor.AnyPost(), "Expected a POST to the flights API");
```

---

## Test Types Planned

### 1. Smoke tests (per area)
One class per page, inheriting area base. Navigate + verify title + run accessibility audit.

```csharp
[TestClass]
public class MembersTests : AdminTestBase
{
    [TestMethod]
    [PageTest("/Admin/Members", "Smoke + a11y")]
    public async Task Index_Loads()
    {
        await Page.GotoAsync($"{TestSettings.CmsWebUrl}/Admin/Members");
        await Expect(Page).ToHaveTitleAsync(new Regex(".+"));
        var a11y = await CheckAccessibility();
        Assert.AreEqual(0, a11y.Violations.Count, string.Join("\n", a11y.Violations));
    }
}
```

### 2. Authorization tests
Verify wrong-role users are redirected to login. One class per protected area,
parameterized with `[DataRow]`.

```csharp
[TestCategory("Authorization")]
public class AdminAuthTests : AuthenticatedPageTest
{
    protected override string AuthStateFile => TestSettings.AuthStateMember; // wrong role

    [TestMethod]
    [DataRow("/Admin/Members")]
    [DataRow("/Admin/Flights25")]
    public async Task AdminPage_AsMember_RedirectsToLogin(string url)
    {
        await Page.GotoAsync($"{TestSettings.CmsWebUrl}{url}");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Login.*"));
    }
}
```

### 3. End-to-end: Flight registration
**Status: Deferred — needs flight form investigation first**

**Key decisions made:**
- Single auth context: Admin user IS also the pilot (TestUserAdminMember)
- No dual-context needed
- Delete flight at end of test (tests delete functionality too)
- Verify account balance before, after registration, and after delete

**Known test data (to be filled in when building):**
- Aircraft registration: TBD (stable in test environment)
- Flight type: TBD
- Expected cost based on known tariff: TBD
- Any required co-pilot, tow pilot, instructor: TBD

**Flow:**
1. Admin registers flight (pilot = test user)
2. Verify flight on `/UserPages/FlightsIndex` with correct cost
3. Verify account balance updated on `/UserPages`
4. Delete flight via Admin
5. Verify flight gone + account balance restored

**Before building:** Read `BlueSky/Pages/Admin/Flights25/` to understand:
- Single page or multi-step form?
- Does save immediately update member account?
- What fields are required?

---

## LLM Role in Testing

Three-layer model:

| Layer | Who | What |
|-------|-----|-------|
| Discovery | Playwright MCP | Navigates live app, finds real selectors, generates page objects and smoke stubs |
| Business rules | Developer | Defines what "correct" means — costs, account balances, role access |
| Review | LLM reviewer | Reads completed tests, identifies missing edge cases and weak assertions |

The review step can be run on any test file:
> "Review this test. What could pass while the feature is still broken?"

### AI-assisted test repair (future)
When a selector breaks after a UI change: pass the failing test + current page HTML
to Claude → get suggested selector fix. Closes the "tests go stale" problem.

---

## Coverage Report
Generated at `PlaywrightTests/coverage-report.html` after each full test run.
- **Coverage section**: total pages, covered, failing, coverage %
- **Accessibility section**: pages audited, clean, violations
- **Per-page rows**: test results, artifact links (trace/video on failure), a11y violations
- **Filter buttons**: All / Covered / Not Covered / Failing / A11y Issues

## Artifacts
All artifacts written to `artifacts/` folder (git-ignored):
- `{TestName}.trace.zip` — Playwright trace (failure only)
- `{uuid}.webm` — video recording (failure only, passed videos auto-deleted)

## WebCare Area
`/Super/WebCare/**` requires `SuperAdmin` role.
No test user configured for this role. **Deferred.**
