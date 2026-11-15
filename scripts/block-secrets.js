// PreToolUse hook (Read, Edit, Write, Bash): block anything that touches a secret file.
// Layer 2 of 2. Layer 1 is the Read deny rules in .claude/settings.json, which current Claude Code
// also applies to files that shell commands read (so `cat .env` is usually stopped there first).
// This hook still blocks when a rule does not match, for example a path written another way.
// Exit code 2 blocks the call and tells Claude why. Node runs on Windows, macOS and Linux.
let input = "";
process.stdin.on("data", d => (input += d));
process.stdin.on("end", () => {
  let tool = "", cmd = "", file = "";
  try {
    const j = JSON.parse(input);
    tool = j.tool_name ?? "";
    cmd = j.tool_input?.command ?? "";
    file = j.tool_input?.file_path ?? j.tool_input?.path ?? "";
  } catch { process.exit(0); }
  const secret = /(^|[^\w])\.env(\.[\w]+)?([^\w.]|$)|secrets[\/\\]|id_rsa|\.pem\b/;
  const text = (tool === "Bash" ? cmd : file).replaceAll(".env.example", "");
  if (secret.test(text)) {
    console.error("Blocked by hook: this touches a secret file (.env, secrets/, keys). Ask a person to check it.");
    process.exit(2);
  }
  process.exit(0);
});
