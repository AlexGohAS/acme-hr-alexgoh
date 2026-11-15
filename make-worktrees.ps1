# Lab 4: two features in parallel, one folder and one branch each.
$ErrorActionPreference = 'Stop'
$base = git rev-parse --abbrev-ref HEAD
git worktree add ../acme-wt-calendar -b feature/leave-calendar $base
if ($LASTEXITCODE -ne 0) { Write-Host "git worktree add failed (see above). Nothing else was done." -ForegroundColor Red; exit 1 }
git worktree add ../acme-wt-holidays -b feature/public-holidays $base
if ($LASTEXITCODE -ne 0) { Write-Host "git worktree add failed (see above)." -ForegroundColor Red; exit 1 }
Write-Host "Each worktree is a fresh folder: run 'cd web; npm install' in ../acme-wt-calendar before building the page."
Write-Host "Git does not copy ignored files: the worktrees have no .env, web/.env.local or .claude/settings.local.json."
Write-Host "  Features A and B do not need them. If one does, copy the file by hand (never through Claude)."
Write-Host "Stop the web dev server in the main folder first: only one app can use port 3000."
Write-Host "Claude in a worktree asks to approve the 'render' MCP server: you can decline it for Features A and B."
Write-Host "Open two terminals (start Claude with: claude --permission-mode auto):"
Write-Host "  cd ../acme-wt-calendar; claude     # Feature A: leave calendar page"
Write-Host "  cd ../acme-wt-holidays; claude     # Feature B: public holidays endpoint"
Write-Host "When both are done: git merge feature/leave-calendar feature/public-holidays"
