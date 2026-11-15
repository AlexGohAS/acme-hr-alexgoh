# PostToolUse hook (Windows): run tests after every edit. Exit 2 sends the failure back to Claude.
# In settings.json use: "command": "powershell -NoProfile -File \"$CLAUDE_PROJECT_DIR/.claude/hooks/run-tests.ps1\""
# (a relative path stops working when Claude's folder moves, for example after "cd web").
if ($env:CLAUDE_PROJECT_DIR) { Set-Location $env:CLAUDE_PROJECT_DIR }
$file = ""
try { $j = [Console]::In.ReadToEnd() | ConvertFrom-Json; $file = "$($j.tool_input.file_path)" } catch {}
if ($file -notmatch '\.(cs|csproj|ts|tsx)$') { exit 0 }   # only after code edits
$out = dotnet test --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
  [Console]::Error.WriteLine("Tests failing after this edit:")
  $out | Select-String -Pattern "\[FAIL\]|error CS|Assert\.|Expected|Actual|Failed!" | Select-Object -First 20 | ForEach-Object { [Console]::Error.WriteLine($_) }
  exit 2
}
exit 0
