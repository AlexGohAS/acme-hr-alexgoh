# Lab 4 — Developer Productivity with Claude (30 min)

**Goal:** make Claude Code a team tool: a Skill, an MCP connection to your Render account, and two features built in parallel.

**Branch:** `git checkout lab4-start` · Solution: `lab5-start`. Prompts: [PROMPTS.md](../../PROMPTS.md#lab-4--team-productivity).

## Before you start
- You have a Render account (sign-up was in the prep guide) and you are **signed in to Render in your browser**. Claude never signs in for you.
- No API key needed. The repo's `.mcp.json` connects to Render's MCP server (`https://mcp.render.com/mcp`) and you approve access in the browser (OAuth).
- You can use the Render plugin instead (`/plugin install render@claude-plugins-official`). Then choose scope **local** or **user**, never project (project writes into `.claude/settings.json`, a tracked file), run `/reload-plugins` and approve the Render page in the browser. Use the plugin or the repo server, not both.
- Signing in gives Claude access to your whole Render workspace. `.claude/settings.json` **denies** every Render tool that creates or updates something, except `create_postgres`, which **asks** you first. The rules cover both names: the repo server (`mcp__render__…`) and the plugin (`mcp__plugin_render_render__…`).
- Render allows **one free Postgres per workspace**, and it **expires after 30 days**.

## Steps
4.1 `git checkout lab4-start`, start `claude` and approve the `render` server once. Run `/mcp` → `render` → **Authenticate**, approve in the browser. `/mcp` then shows `render` connected. (`.mcp.json` names Render's registered client, `"oauth": { "clientId": "claude" }`; without it Render answers "Incompatible auth server: does not support dynamic client registration".)

4.2 Paste **Create the database**. Confirm the workspace when Claude asks, and approve the `create_postgres` call only if it shows plan `free`, region `singapore`, version `16`. The database stays in `creating` for several minutes: this is normal.

4.3 **Allow your laptop** (manual; the MCP server cannot change the IP allow list). Render dashboard → `acme-hr-db` → **Info** → scroll to **PostgreSQL Inbound IP Rules** (it says "All internet traffic is blocked by PostgreSQL inbound IP rules") → **+ Add source** → SOURCE: your public IP with `/32` (open https://icanhazip.com), DESCRIPTION: `my laptop` → **Save**.
   - Do not use `0.0.0.0/0` unless the trainer says so.
   - The page also shows the **workspace** inbound rules (`0.0.0.0/0 everywhere`). Those are not the blocker; the database's own list is.
   - Careful: the red **Delete Database** button is just below this section.

4.4 **Copy the URL yourself** (manual; Claude never handles it). `acme-hr-db` → **Connect** → **External** (not Internal: the Internal URL works only inside Render) → copy the URL. Paste it into `.env` as `DATABASE_URL` in your editor, never in chat. Without step 4.3 the External tab is switched off.
   - In the same `.env`, set `SEED_USER_PASSWORD` to a private password of your own (required: on Postgres the API refuses to start without it, or with `Acme@2026`). The API creates the logins in the new database with it on the next start, and in Lab 5 your live app uses this database. From now on, sign in with this password (your local SQLite keeps `Acme@2026`).

4.5 Paste **Check the database**: `/health` must show `"provider":"Npgsql.EntityFrameworkCore.PostgreSQL"`, and `Acme@2026` must get 401. The new database starts with fresh seed data; your local SQLite leave requests are not copied. Then paste **MCP question**: Claude reports the database through MCP without SQL. Render's MCP server has no fixed IP, so your allow list (only your laptop) keeps it out of the data: that is the allow list working.

4.6 Paste **Check the guardrail**. The expected result: `update_environment_variables` is **not in Claude's tool list** (a matching deny rule removes the tool, "Denied by a permission rule"), so Claude cannot call it. If Render itself answers ("not found"), the deny rules do not match your tool names: tell a trainer.

4.7 Stop the web dev server in the main folder (it holds port 3000). Run `./make-worktrees.sh` (Windows: `.\make-worktrees.ps1`). It makes `../acme-wt-calendar` and `../acme-wt-holidays`.
   - Git does not copy ignored files: the worktrees have no `.env`, no `web/.env.local` and no `.claude/settings.local.json`. Features A and B do not need them. If you want to run the API there, copy `.env` yourself.
   - In the calendar worktree run `npm --prefix web install` first (each worktree is a fresh folder).

4.8 Open two terminals and start Claude in **auto mode** in each, so the two sessions can work without stopping for every approval: `claude --permission-mode auto` (or Shift+Tab until the footer shows auto). Accept the trust dialog: each worktree is a new folder. When asked to approve the `render` server there, you can decline it. The deny rules (`.env`, Render writes) still apply in auto mode.
   - `../acme-wt-calendar` → paste **Feature A** (leave calendar page)
   - `../acme-wt-holidays` → paste **Feature B** (public holidays endpoint)
   - When a feature is done, paste **Commit the feature** in that worktree.

4.9 Back in the main folder, stop the API, then `git merge feature/leave-calendar feature/public-holidays`. Run `dotnet test` and `npm --prefix web run build`. Then paste **Release notes** and check Claude used the `release-notes` Skill.

4.10 Restart the API and web app. As Aina (your private password) open **Calendar**; in Swagger call the public holidays endpoint.

## Done when
- [ ] `acme-hr-db` created through MCP on the free plan, after you approved it
- [ ] `/health` shows the Npgsql provider, `Acme@2026` gets 401, and MCP reports the database status
- [ ] The Render write tool was removed by a deny rule
- [ ] Both features merged with tests
- [ ] Release notes follow the Skill's format

## Stuck?
- No External URL under Connect: the database's IP allow list is empty. Do step 4.3.
- "Incompatible auth server" in `/mcp`: your `.mcp.json` lacks the `oauth` client id; take it from the template's `lab4-start`.
- Created the database in the dashboard instead of MCP? It starts with `0.0.0.0/0` (open to the internet), PostgreSQL 18 and a paid plan. Pick **Free** and version **16**, and replace the rule with your `/32`.
- API does not start: "SEED_USER_PASSWORD must be set": set your private password in `.env`. "seeded with the public training password": delete and recreate the database (it was first started without a private password).
- `create_postgres` says "version is required": pass version 16.
- "Free tier limit": one free database per workspace. Delete the old one or use another workspace.
- Tests still pass against SQLite after 4.4: correct. Tests never use `.env`, so they never touch Render.
- Chrome fallback (only if MCP is not possible): sign in to Render first, then ask Claude to run `list_connected_browsers` and use the Chrome on **this** computer; if more than one browser is listed, it must ask you which. If screenshots fail with "Cannot access a chrome-extension:// URL", pause your password-manager extension.
- `next dev` fails on port 3000 in a worktree: stop the main folder's dev server. On Windows another app may also use `127.0.0.1:3000`; open `http://localhost:3000`.
- Render down on the day: the trainer may switch to Vercel + Neon. See [docs/deployment-alternative.md](../../docs/deployment-alternative.md).

## Debrief
What in your codebase would a Skill capture, and what would an MCP server connect?
