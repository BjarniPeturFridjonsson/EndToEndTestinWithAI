import express from 'express';
import { chromium } from 'playwright';
import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const app = express();
app.use(express.json({ limit: '5mb' }));
app.use(express.static(path.join(__dirname, 'public')));

const TESTS_PATH = path.resolve(__dirname, '../PlaywrightTests');
const PAGE_LIST_PATH = path.resolve(__dirname, '../PlaywrightTests/PAGE-LIST.md');
const SETTINGS_PATH = path.resolve(__dirname, '../PlaywrightTests/testsettings.json');

// Load from testsettings.json (same file the MSTest project uses)
const settings = JSON.parse(await fs.readFile(SETTINGS_PATH, 'utf-8'));
const CMS_URL = settings.CmsWebUrl;

// --- Browser management ---

let browser = null;
const contexts = {}; // one persistent logged-in context per role

const credentials = {
  Admin:    { user: settings.Users.Admin.Username,    pass: settings.Users.Admin.Password },
  Super:    { user: settings.Users.Super.Username,    pass: settings.Users.Super.Password },
  Training: { user: settings.Users.Training.Username, pass: settings.Users.Training.Password },
  member:   { user: settings.Users.Member.Username,   pass: settings.Users.Member.Password },
};

async function getBrowser() {
  if (!browser) browser = await chromium.launch({ headless: true });
  return browser;
}

async function getContext(authState) {
  if (contexts[authState]) return contexts[authState];

  const b = await getBrowser();
  const ctx = await b.newContext({ ignoreHTTPSErrors: true });

  if (authState !== 'none') {
    const creds = credentials[authState];
    if (!creds?.user) throw new Error(`No credentials configured for role: ${authState}`);
    const page = await ctx.newPage();
    await page.goto(`${CMS_URL}/Login`, { waitUntil: 'networkidle' });
    await page.fill("input[name='Username']", creds.user);
    await page.fill("input[name='password']", creds.pass);
    await page.click("button[type='submit'], input[type='submit']");
    await page.waitForURL('**/UserPages**', { timeout: 10000 }).catch(() => {});
    await page.close();
  }

  contexts[authState] = ctx;
  return ctx;
}

// --- Page navigation & extraction ---

async function navigatePage(pagePath, authState) {
  const url = `${CMS_URL}/${pagePath.replace(/^\//, '')}`;
  const ctx = await getContext(authState);
  const page = await ctx.newPage();
  try {
    await page.goto(url, { waitUntil: 'networkidle', timeout: 15000 });

    const title = await page.title();
    const finalUrl = page.url();
    const screenshot = `data:image/png;base64,${(await page.screenshot({ fullPage: false })).toString('base64')}`;

    const elements = await page.evaluate(() => {
      const items = [];
      document.querySelectorAll('h1,h2,h3,h4,h5').forEach(el => {
        const text = el.textContent.trim();
        if (text) items.push({ kind: 'heading', tag: el.tagName, text });
      });
      document.querySelectorAll('button,[type="submit"],a.btn,a.page-link').forEach(el => {
        const text = el.textContent.trim();
        if (text) items.push({ kind: 'button', text, id: el.id || null });
      });
      document.querySelectorAll('input:not([type="hidden"]),select,textarea').forEach(el => {
        const label = document.querySelector(`label[for="${el.id}"]`)?.textContent?.trim() || null;
        items.push({ kind: 'input', inputType: el.type || el.tagName.toLowerCase(), id: el.id || null, name: el.name || null, label });
      });
      document.querySelectorAll('table').forEach((t, i) => {
        const headers = [...t.querySelectorAll('thead th')].map(th => th.textContent.trim()).filter(Boolean);
        if (headers.length) items.push({ kind: 'table', index: i, headers });
      });
      return items;
    });

    return { title, url: finalUrl, screenshot, elements };
  } finally {
    await page.close();
  }
}

// --- PAGE-LIST.md parser ---

function parsePageList(content) {
  const sections = [];
  let current = null;

  for (const line of content.split('\n')) {
    const sectionMatch = line.match(/^## (.+)/);
    if (sectionMatch) {
      current = {
        title: sectionMatch[1].replace(/[🔑👤🛠️📋🔒🚫⭐]/gu, '').trim(),
        authState: 'none',
        baseClass: null,
        pages: []
      };
      sections.push(current);
      continue;
    }
    if (!current) continue;

    const authMatch = line.match(/Auth state: `(\w+)`/);
    if (authMatch) {
      const map = { AuthStateMember: 'member', AuthStateAdmin: 'Admin', AuthStateSuper: 'Super', AuthStateTraining: 'Training' };
      current.authState = map[authMatch[1]] || 'none';
    }

    const baseMatch = line.match(/Base class: `(\w+)`/);
    if (baseMatch) current.baseClass = baseMatch[1];

    const pageMatch = line.match(/^- \[([ x])\] `([^`]+)`\s*(?:—\s*(.+))?/);
    if (pageMatch) {
      const desc = pageMatch[3] || '';
      current.pages.push({
        covered: pageMatch[1] === 'x',
        path: pageMatch[2],
        description: desc.replace(/[⚠️⭐]/gu, '').trim(),
        warning: desc.includes('⚠️'),
        key: desc.includes('⭐'),
      });
    }
  }
  return sections.filter(s => s.pages.length > 0);
}

// --- Existing test scanner ---

async function findExistingTests() {
  const found = new Set();
  const testsDir = path.join(TESTS_PATH, 'Tests');
  try {
    for (const area of await fs.readdir(testsDir)) {
      const areaPath = path.join(testsDir, area);
      if (!(await fs.stat(areaPath)).isDirectory()) continue;
      for (const file of await fs.readdir(areaPath)) {
        if (file.endsWith('Tests.cs')) found.add(`${area}/${file.replace('Tests.cs', '')}`);
      }
    }
  } catch { /* tests dir may not exist yet */ }
  return found;
}

// --- Routes ---

app.get('/api/pages', async (req, res) => {
  try {
    const content = await fs.readFile(PAGE_LIST_PATH, 'utf-8');
    const sections = parsePageList(content);
    const existing = await findExistingTests();

    for (const section of sections) {
      for (const page of section.pages) {
        const [area, ...rest] = page.path.split('/');
        if (existing.has(`${area}/${rest.join('')}`)) page.covered = true;
      }
    }

    const total = sections.reduce((n, s) => n + s.pages.length, 0);
    const covered = sections.reduce((n, s) => n + s.pages.filter(p => p.covered).length, 0);
    res.json({ sections, total, covered });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.post('/api/navigate', async (req, res) => {
  const { path: pagePath, authState } = req.body;
  if (!pagePath) return res.status(400).json({ error: 'path required' });
  try {
    res.json(await navigatePage(pagePath, authState || 'none'));
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.post('/api/save-test', async (req, res) => {
  const { content, pagePath } = req.body;
  if (!content || !pagePath) return res.status(400).json({ error: 'content and pagePath required' });

  const [area, ...rest] = pagePath.split('/');
  const filename = rest.join('') + 'Tests.cs';
  const dir = path.join(TESTS_PATH, 'Tests', area);
  const filePath = path.join(dir, filename);

  try {
    await fs.mkdir(dir, { recursive: true });
    await fs.writeFile(filePath, content, 'utf-8');
    res.json({ saved: true, path: filePath, filename });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// Graceful shutdown — close browser on exit
process.on('SIGINT', async () => {
  if (browser) await browser.close();
  process.exit(0);
});

const PORT = process.env.PORT || 3333;
app.listen(PORT, () => {
  console.log(`Test Builder running at http://localhost:${PORT}`);
  console.log(`App URL: ${CMS_URL}`);
  console.log(`Tests: ${TESTS_PATH}`);
});
