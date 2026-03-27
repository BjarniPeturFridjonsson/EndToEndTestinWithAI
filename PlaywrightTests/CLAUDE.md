# PlaywrightTests — Testing Rules

Rules to follow when writing or modifying Playwright tests in this project.

## 1. Suggest UI improvements when locators are fragile

If a test requires a workaround (XPath siblings, Nth(), positional selectors, very short GetByText strings),
flag it and suggest adding `id`, `aria-label`, `data-testid`, `href="#"`, or a semantic HTML fix to the page.
Tests reveal UX problems — surface them.

## 2. Prefer stable locators (in priority order)

1. `[name='...']` — for form inputs, matches the model binding name, very reliable
2. `GetByLabel` — for labeled fields
3. `GetByRole` + Name — for buttons and links (requires `href` on anchors to be recognized as links)
4. `GetByText` — only for unique strings of 4+ chars
5. XPath / positional — last resort, add a comment explaining why

## 3. Always revert test data

Use try/finally to restore original field values. Capture originals before making any changes.
Tests must leave the database in the same state they found it.

## 4. The redirect IS the proof

`WaitForURLAsync("**/DetailsPage**")` succeeding means the server saved to the API and redirected —
the full round-trip is confirmed. No need to assert every field on the details page.
Spot-check one key field at most if you want extra confidence.

## 5. Use test strings that are long enough and within field validation limits

Short values (2–3 chars) cause fragile `GetByText` matching. Use 4–6+ chars.
Check the page model for validation rules before choosing test values — e.g.:
- `NameInitials`: 2–6 chars
- `NameShort`: 4–11 chars

## 6. MCP browser — app URL and session recovery

The app runs at **`http://localhost:60295`** (from `testsettings.json` → `CmsWebUrl`).

When the MCP browser shows `about:blank` (dropped session), navigate there before exploring:
```
mcp__playwright__browser_navigate → http://localhost:60295
```

Admin/Super pages require login. Use `/Login` with:
- Super: `TestUserSuperMember@localtest.com` / `!123456`
- Admin: `TestUserAdminMember@localtest.com` / `!123456`
- Member:    `TestUserRegularMember@localtest.com` / `!123456`
