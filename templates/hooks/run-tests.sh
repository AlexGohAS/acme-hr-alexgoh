#!/usr/bin/env bash
# PostToolUse hook: run tests after every edit. Exit 2 sends the failure back to Claude.
# Change TEST_CMD to your project's test command.
TEST_CMD=${TEST_CMD:-"dotnet test --nologo -v q"}
# Run from the project root, whatever folder Claude is in (a relative path breaks after "cd web").
cd "${CLAUDE_PROJECT_DIR:-.}" || exit 0
# Only after code edits: skip docs, settings and plans (edit the pattern for your stack).
file=$(node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>{try{const i=JSON.parse(s).tool_input||{};console.log(i.file_path||i.path||"")}catch{console.log("")}})')
case "$file" in *.cs|*.csproj|*.ts|*.tsx) ;; *) exit 0 ;; esac
out=$($TEST_CMD 2>&1)
if [ $? -ne 0 ]; then
  echo "Tests failing after this edit:" >&2
  echo "$out" | grep -E "\[FAIL\]|error CS|Assert\.|Expected|Actual|Failed!|FAIL" | head -20 >&2
  exit 2
fi
exit 0
