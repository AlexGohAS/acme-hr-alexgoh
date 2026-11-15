# Lab 2 — Customer Support Resolution Agent: HR helpdesk (35 min)

**Goal:** add an HR helpdesk agent to Acme HR using the Claude API, safe by code, not by prompt.

**Branch:** `git checkout lab2-start` (contains the finished Lab 1) · Solution: `lab3-start`. Prompts: [PROMPTS.md](../../PROMPTS.md#lab-2--hr-helpdesk-agent).

## Steps
2.1 Stop the API, `git checkout lab2-start` and run `dotnet test`: 5 helpdesk tests fail. The branch has the course solution of Lab 1, not your own code; commit your Lab 1 work on a branch of your own first if you want to keep it. Read `src/Api/Helpdesk/HelpdeskAgent.cs` and `HelpdeskTools.cs`.

2.2 Keep the API stopped while Claude builds. Paste **Finish the agent loop**. The loop resends a copy of the history, checks `StopReason`, returns one `tool_result` per `tool_use` (also when a tool throws) and stops at `MaxTurns`. The 4 tools: `get_leave_balance`, `apply_leave`, `check_policy`, `escalate_to_hr`. The rule lives in `apply_leave`: more than 3 working days returns `needs_manager_approval` and creates a Pending request.

2.3 Stop and restart the API (it reads `.env` at start), then paste **Run the tickets**. The runner picks a free week, so you can run it again; it rejects its Pending request at the end.

2.4 As Farah, open **Helpdesk** and ask: "How many days of annual leave do I have left?" Then: "Apply for leave 7 to 11 December." The balance matches the home page, and the 5-day request comes back as needing manager approval and shows as Pending on **Leave**.

## Done when
- [ ] Every one of the 10 tickets gets an outcome
- [ ] The 5-day request returns `needs_manager_approval` from code
- [ ] Ticket 7 (prompt injection) is refused and creates no leave
- [ ] No run goes past `MaxTurns`

## Stuck?
- 401 / invalid key: `ANTHROPIC_API_KEY` missing in `.env`. Restart the API.
- "tool_use ids without tool_result": return one tool_result for every tool_use, even when the tool fails.
- Loops forever: you did not add Claude's own turn to the history.
- Windows build fails with MSB3027: stop the API before `dotnet test`.

## Debrief
Which rule did you put in the prompt, and which in code? Why?
