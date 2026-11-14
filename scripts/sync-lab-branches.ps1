# Run ONCE, right after you clone the repo you made with "Use this template".
# GitHub's template copy gives each branch its own one-commit history, so pull
# requests between branches (Lab 5, capstone) fail with "entirely different
# commit histories". This replaces your branches with the template's real history.
# Run it before you change anything. Your .env is not touched.
$ErrorActionPreference = 'Stop'
$template = 'https://github.com/anchor-academy/acme-hr-workshop-net.git'
$branches = 'main','lab1-start','lab2-start','lab3-start','lab4-start','lab5-start','lab5-planted-bug','lab6-start','final'
git remote get-url template 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { git remote add template $template }
git fetch --quiet template
foreach ($b in $branches) {
  git push --quiet --force origin "refs/remotes/template/$($b):refs/heads/$($b)"
  if ($LASTEXITCODE -ne 0) { throw "push of $b failed" }
  Write-Host "  synced $b"
}
git fetch --quiet --prune origin
git checkout --quiet main
git reset --quiet --hard origin/main
Write-Host "Done. Your branches now share history, so Lab 5 pull requests work."
