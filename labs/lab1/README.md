# Lab 1 — Code Generation with Claude Code: the leave API (35 min)

**Goal:** build the leave feature of Acme HR with Claude Code, under team rules that hold on their own.

**Branch:** `git checkout lab1-start` · Solution: `lab2-start` contains the finished Lab 1. Prompts: [PROMPTS.md](../../PROMPTS.md#lab-1--leave-api).

## Steps
1.1 `git checkout lab1-start` and run `dotnet test`. One test fails: the weekend working-days bug. Read `CLAUDE.md`, `.claude/rules/` and `.claude/settings.json`. Which lines are instructions, and which are enforced?

1.2 Start `claude` (accept the trust dialog the first time), switch to plan mode (Shift+Tab until the footer says plan mode), paste **Plan prompt**. Review the plan and ask for one change.

1.3 Approve and paste **Build it**. Watch the test hook run after each edit to a `.cs` file.

1.4 Paste **Try to break the rules**. It tests the protection with `src/.env`, a file that does not exist, from the repo root and from `web/`. All 4 attempts must be stopped. Either message counts: "denied by your permission settings" (a deny rule) or "Blocked by hook". Three layers are at work: the CLAUDE.md instruction (Claude may refuse on its own), the `Read` deny rules and the PreToolUse hook `scripts/block-secrets.js`. Only the last two are enforced.
   - If any attempt says "File does not exist" or prints `hook-test`, the protection is off: stop and call a trainer.
   - Never test with your real `.env`. If a protection fails, your API key goes into the session and you must rotate it in the Console.

1.5 Run the API and the web app (Windows: stop the API before `dotnet test`). Sign in as Farah (`farah.tan6@acme-hr.example`) and on **Leave** create:
   - Thu 3 Dec – Fri 4 Dec 2026: 2 days, **Approved** at once
   - Mon 14 Dec – Fri 18 Dec 2026: 5 days, **Pending**

   Sign in as Gopal (`gopal.ng7@…`): he cannot see or approve them (403). Sign in as Aina (`aina.lee1@…`), Farah's manager, and approve the 5-day request.

## Done when
- [ ] The plan was reviewed before any code
- [ ] All tests pass, including the weekend test that failed
- [ ] The Leave page creates requests and shows each request's days, its status and the available balance
- [ ] Farah cannot approve her own request (403), Gopal gets 403, Aina can
- [ ] All 4 attempts in 1.4 were stopped by a deny rule or the hook

## Stuck?
- Hook keeps failing: read the test names it prints; ask Claude to fix those tests first.
- Hook says "Tests failing" with no test names, or the build fails with MSB3027 (Windows): the running API locks the build. Stop the API, then run the tests.
- API not reachable from the web app: is the API running on port 5080? Is `web/.env.local` set?

## Debrief
Which of these controls would you add to your own repos on Monday?
