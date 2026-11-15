#!/usr/bin/env bash
# PostToolUse hook: after Claude edits C# code in src/, run the .NET tests from the repo root.
# Exit 2 sends the result back to Claude. Other edits (docs, web, plan files) skip the tests.
input=$(cat)
file=$(printf '%s' "$input" | node -e 'let s="";process.stdin.on("data",d=>s+=d).on("end",()=>{try{const j=JSON.parse(s);process.stdout.write(String((j.tool_input&&j.tool_input.file_path)||""))}catch(e){}})')
f="${file//\\//}"
case "$f" in
  */.claude/*) exit 0 ;;
  */src/*.cs|*/src/*.csproj|src/*.cs|src/*.csproj) ;;
  *) exit 0 ;;
esac
root="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "$0")/.." && pwd)}"
cd "$root" || exit 0
# One run at a time: several quick edits must not start several test runs.
lock="$root/.test-hook.lock"
if ! mkdir "$lock" 2>/dev/null; then exit 0; fi
trap 'rmdir "$lock" 2>/dev/null' EXIT
out=$(dotnet test --nologo -v q 2>&1)
if [ $? -ne 0 ]; then
  echo "Tests failing after this edit. If you are writing a test first (red step), this is expected: go on and write the code. Otherwise fix the code, not the test." >&2
  echo "$out" | grep -E "\[FAIL\]|error CS|Assert\.|Expected|Actual|Failed!|MSB3026|MSB3027|MSB3021" | grep -v "^\s*at " | head -30 >&2
  if echo "$out" | grep -qE "MSB3026|MSB3027|MSB3021"; then
    echo "The build could not replace Api.exe: the API is still running. Stop it (Ctrl+C in its terminal), then run the tests again." >&2
  fi
  exit 2
fi
exit 0
