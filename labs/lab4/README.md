# Lab 4 — Developer Productivity with Claude (30 min)

**Goal:** make Claude Code a team tool: a Skill, a read-only MCP connection to your data, and two features built in parallel.

**Branch:** `git checkout lab4-start` · Solution: `lab5-start`.

## Before you start
- No API key needed. The repo's `.mcp.json` connects to Render's MCP server (`https://mcp.render.com/mcp`), and you sign in with your Render account in the browser (OAuth). This is the same server the Render plugin uses (`/plugin install render@claude-plugins-official`); use one or the other, not both.
- Signing in gives Claude access to your whole Render workspace, so `.claude/settings.json` **denies** every Render MCP tool that creates or updates something, except `create_postgres`, which **asks** you first. The rules cover both the repo server (`mcp__render__…`) and the plugin (`mcp__plugin_render_render__…`).

## Steps
1. Start `claude` and approve the `render` server once. Run `/mcp` → `render` → **Authenticate**, sign in to Render in the browser and approve. `/mcp` should then show `render` connected.
   - Paste **Create the database**. Approve the `create_postgres` call when Claude asks. Check it says plan `free`, region `singapore`.
   - When it is available: in the Render dashboard, `acme-hr-db` → Connect → copy the **External Database URL** into `.env` as `DATABASE_URL` yourself (Claude never handles it). Restart the API once: it creates and seeds the tables.
   - Paste **MCP question**.
   - Paste **Check the guardrail**. The call must be blocked by a permission rule, not answered by Render. If Render answers, your deny rules do not match the tool names: tell a trainer.
2. Run `./make-worktrees.sh` (or `.ps1`). Open two terminals and start Claude in **auto mode** in each, so the two sessions can work without stopping for every approval: `claude --permission-mode auto` (or press Shift+Tab until the footer shows auto). Accept the trust dialog: each worktree is a new folder. The deny rules (`.env`, Render writes) still apply in auto mode.
   - `../acme-wt-calendar` → paste **Feature A** (leave calendar page)
   - `../acme-wt-holidays` → paste **Feature B** (public holidays endpoint)
   - In the calendar worktree run `cd web && npm install` first (each worktree is a fresh folder).
   - When a feature is done, paste **Commit the feature** in that worktree.
3. Back in the main folder: `git merge feature/leave-calendar feature/public-holidays`. Run `dotnet test` and `npm run build` in `web/`.
4. Paste **Release notes**. Check Claude used the `release-notes` Skill.

## Done when
- [ ] `acme-hr-db` created through MCP on the free plan, after you approved it
- [ ] MCP answers from your live Render Postgres data, with a read-only query
- [ ] A Render write was blocked by a permission rule
- [ ] Both features merged with tests
- [ ] Release notes follow the Skill's format

## Debrief
What in your codebase would a Skill capture, and what would an MCP server connect?
