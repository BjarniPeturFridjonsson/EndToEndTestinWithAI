# TestBuilderTool — Design Document

## Purpose

A local developer tool that combines Playwright browser automation, the Claude AI API,
and live knowledge of the test project structure to help write Playwright tests through
a conversational web UI.

This is a demonstration of AI-assisted test authoring:
- Playwright MCP navigates the live app and discovers real selectors
- The developer provides business rules through a chat dialogue
- Claude generates complete, compilable C# test classes that fit the existing infrastructure
- The server writes the approved file directly to the PlaywrightTests project

---

## Architecture

```
TestBuilderTool/
├── DESIGN.md          This file
├── package.json       Node.js dependencies
├── .env               Local config (git-ignored)
├── .env.example       Config template
├── server.js          Express server (API + static files)
└── public/
    └── index.html     Single-page UI
```

The tool runs as a local Express server on port 3333.
It is separate from the main .NET solution and has no production role.

---

## Server Endpoints

| Method | Path               | Description                                              |
|--------|--------------------|----------------------------------------------------------|
| GET    | /api/pages         | Parse PAGE-LIST.md, scan existing tests, return combined |
| POST   | /api/navigate      | Playwright navigates a URL, returns screenshot + elements|
| POST   | /api/chat          | Stream Claude API response (SSE)                         |
| POST   | /api/save-test     | Write a .cs file to PlaywrightTests/Tests/{area}/        |

---

## Workflow

1. **Start the app** — the .NET app must be running (e.g. https://localhost:7001)
2. **Start the tool** — `npm start` in `TestBuilderTool/`
3. **Open browser** — http://localhost:3333
4. **Pick a page** from the sidebar (grouped by area, green = already has a test)
5. **Click Navigate** — server launches Playwright, logs in as the correct role, takes a screenshot and extracts interactive elements
6. **Chat** — describe what to test ("the save button should POST to the API", "only ClubAdmin can see this page")
7. **Review generated test** — Claude writes a complete C# test class using the correct base class, `[PageTest]` attribute, and `CheckAccessibility()`
8. **Save** — click Save Test, the server writes the file to `PlaywrightTests/Tests/{area}/`
9. **Build and run** — `dotnet test PlaywrightTests/PlaywrightTests.csproj --filter "TestCategory=ClubAdmin"`

---

## Authentication

The tool logs in as each role using credentials from `.env`.
After first login per role, the browser context (cookies/session) is reused for
subsequent navigations in the same server session — no repeated logins.

Login form selectors (from existing GlobalSetup):
- Username: `input[name='Username']`
- Password: `input[name='password']`
- Redirects to `/UserPages` on success

---

## Test File Generation

Claude receives:
- The page URL, area, and required auth state
- The page title, headings, buttons, and form fields (extracted live)
- The complete conversation history
- A system prompt containing the test project conventions and a working example

Claude outputs a complete `.cs` file. The server writes it to:
```
../PlaywrightTests/Tests/{Area}/{PageName}Tests.cs
```

### Conventions Claude follows
- Namespace: `PlaywrightTests.Tests.{Area}`
- Base class: `ClubSuperTestBase` / `ClubAdminTestBase` / `ClubTrainingTestBase` / `UserPagesTestBase`
- `[TestClass]` on the class
- `[TestMethod]` + `[PageTest(url, description)]` on each method
- `await CheckAccessibility()` in smoke tests
- `TestSettings.CmsWebUrl` as the base URL

---

## Dependencies

| Package             | Version  | Purpose                         |
|---------------------|----------|---------------------------------|
| express             | ^4.18     | HTTP server                     |
| @anthropic-ai/sdk   | ^0.36     | Claude API client with streaming |
| playwright          | ^1.50     | Browser automation              |
| dotenv              | ^16       | Environment config              |

---

## Configuration (.env)

```
ANTHROPIC_API_KEY=sk-ant-...
CMS_URL=https://localhost:7001
PLAYWRIGHT_TESTS_PATH=../PlaywrightTests
PAGE_LIST_PATH=../PlaywrightTests/PAGE-LIST.md

CLUB_ADMIN_USER=TestUserAdminMember@localtest.com
CLUB_ADMIN_PASS=...
CLUB_SUPER_USER=TestUserSuperMember@localtest.com
CLUB_SUPER_PASS=...
CLUB_TRAINING_USER=TestUserTrainingMember@localtest.com
CLUB_TRAINING_PASS=...
MEMBER_USER=TestUserRegularMember@localtest.com
MEMBER_PASS=...
```
