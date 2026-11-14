#!/usr/bin/env bash
# Run ONCE, right after you clone the repo you made with "Use this template".
# GitHub's template copy gives each branch its own one-commit history, so pull
# requests between branches (Lab 5, capstone) fail with "entirely different
# commit histories". This replaces your branches with the template's real history.
# Run it before you change anything. Your .env is not touched.
set -e
TEMPLATE=https://github.com/anchor-academy/acme-hr-workshop-net.git
BRANCHES="main lab1-start lab2-start lab3-start lab4-start lab5-start lab5-planted-bug lab6-start final"
git remote get-url template >/dev/null 2>&1 || git remote add template "$TEMPLATE"
git fetch --quiet template
for b in $BRANCHES; do
  git push --quiet --force origin "refs/remotes/template/$b:refs/heads/$b"
  echo "  synced $b"
done
git fetch --quiet --prune origin
git checkout --quiet main
git reset --quiet --hard origin/main
echo "Done. Your branches now share history, so Lab 5 pull requests work."
