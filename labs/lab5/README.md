# Lab 5 — Claude Code for Continuous Integration: gates, then go live (30 min)

**Goal:** CI, not the agent's own report, decides "done". Then Acme HR goes live.

**Branch:** `lab5-start`, pushed to your repo's `main` in step 5.1 · Solution: `lab6-start`. Prompts: [PROMPTS.md](../../PROMPTS.md#lab-5--ci).

## Before you start
- Your app goes live on a public address, and the README (public repo) prints the training password. So the live app uses your own private password (`SEED_USER_PASSWORD`, set in Lab 4 step 4.4).
- Set a **spend limit** on the Anthropic key you give to GitHub and Render: anyone who signs in to your live app spends that key.
- Render shows maintenance on **18 Nov, 10:00 (GMT+8)**. If a deploy hangs then, wait and retry.
- Cost: each Claude review is about US$0.20–0.55 (dry run: US$0.19, US$0.32, US$0.54).

## Steps
5.1 Make `lab5-start` your `main`: `git checkout lab5-start`, then `git push origin lab5-start:main --force` (your own repo only), then `git fetch origin` and `git checkout -B main origin/main` so your local `main` matches. `ci` runs on `main`.

5.2 Render → **New** → **Blueprint** → your repo (or **Public Git Repository** with its GitHub URL; Render then says Auto-Deploy is not available, which is fine: the deploy hook deploys). **Blueprint Name:** `acme-hr`. It reads `render.yaml`: one free Docker service in Singapore. Fill the values yourself, never through Claude:
   - `DATABASE_URL`: `acme-hr-db` → Connect → **Internal** Database URL
   - `ANTHROPIC_API_KEY`: your key with a spend limit
   - `SEED_USER_PASSWORD`: the same private password you set in `.env` in Lab 4 step 4.4. It is used only when the database has no logins yet; your Lab 4 run already created them with it. If you skipped that, the live app still has `Acme@2026`: ask a trainer.

   Click **Deploy Blueprint**. Your URL gets a random suffix, e.g. `acme-hr-z7ko.onrender.com`.

5.3 Render → `acme-hr` service → **Settings** → **Deploy Hook**. Every service already has one; there is nothing to create. Copy it, and in your own terminal run `gh secret set RENDER_DEPLOY_HOOK -R <you>/<your-repo>` and `gh secret set ANTHROPIC_API_KEY -R <you>/<your-repo>`, pasting each value when asked. Name the repo with `-R`: without it `gh` picks the repo of the current folder, which may be another one. Check with `gh secret list -R <you>/<your-repo>`: both names must be listed before 5.4. Treat the hook as a secret: if it leaks, click **Regenerate Hook**.

5.4 Paste **Open the pull request** (from `lab5-planted-bug` to `main`). `ci` passes, `claude-review` fails. The review is **one comment in the Conversation tab** (not on the lines in "Files changed"); each finding names file:line. The cost is in the comment footer and on the Actions run page.

5.5 Now that both checks have run once: GitHub → Settings → Branches → protect `main`, require **ci** and **claude-review**. GitHub asks **Confirm access** (password / 2FA) before it saves: do it yourself, never through Claude. Alternative, in your own terminal (replace `<owner>/<repo>`):
   ```bash
   echo '{"required_status_checks":{"strict":false,"contexts":["ci","claude-review"]},"enforce_admins":false,"required_pull_request_reviews":null,"restrictions":null}' | gh api -X PUT repos/<owner>/<repo>/branches/main/protection --input -
   ```
   With `enforce_admins` false, you as the owner could still merge past the checks. That is on purpose for the workshop (it lets a trainer unblock you); don't use it.

5.6 Check out `lab5-planted-bug`, copy the review comment, paste **Fix the planted bug** and replace the placeholder with the comment. The review blocks on more than one thing; the prompt fixes all of them: tests for Approved, Pending and Rejected, a 403 test for another employee and a 404 test for an unknown id. Push (`git push -u origin lab5-planted-bug`), and merge when both checks are green. If the review blocks again, paste its new comment into the same prompt once more.

5.7 Paste **Check the deploy**. A green run is not enough: the deploy step must print `{"deploy":{"id":"dep-..."}}`, and Render → `acme-hr` → **Events** shows a deploy with trigger `deploy_hook`. Look only for `dep-`: the log always shows the script text "RENDER_DEPLOY_HOOK not set", which means nothing. The first deploy takes a few minutes.

5.8 Open `https://<your-service>.onrender.com` and sign in as Farah with your `SEED_USER_PASSWORD`.

5.9 Clean up access: in `.env`, put `#` before `DATABASE_URL` so local work uses SQLite again and never writes into the live database. In Render, delete the `my laptop` IP rule (and any `0.0.0.0/0` rule) from `acme-hr-db`: the service uses the Internal URL. Removing the last rule asks you to type a confirmation to block all internet traffic: that is what you want.

## Done when
- [ ] The review blocked the bad pull request
- [ ] The fix includes tests for Approved, Pending, Rejected, 403 and 404, and is merged
- [ ] The deploy printed a `dep-` id and Render shows trigger `deploy_hook`
- [ ] Acme HR is live: you signed in at `https://<your-service>.onrender.com` with your private password

## Stuck?
- Can't require checks: the repo must be public on GitHub Free, and the checks appear only after they have run once (step 5.4).
- `claude-review` fails and there is no comment on the pull request: the `ANTHROPIC_API_KEY` secret is wrong or on another repo (`gh secret list -R <you>/<your-repo>`). The run log says "key check failed". Set it again and re-run the failed job. A correct review also fails in about 30 seconds: look for the comment, not the time.
- Deploy is green but nothing changed on Render: `RENDER_DEPLOY_HOOK` is not set; the log says "skipping API deploy".
- "Entirely different commit histories" on the PR: the sync script did not run in setup.
- First page load takes about a minute: the free service sleeps after 15 minutes idle. Open `/health` to wake it.
- Deploy fails on the database: use the **Internal** Database URL (same region, Singapore), not the External one.
- Render down on the day: the trainer may switch to Vercel + Neon. See [docs/deployment-alternative.md](../../docs/deployment-alternative.md).

## After the workshop
Suspend or delete the Render service and the free database, and delete or limit the Anthropic key you gave to GitHub and Render.

## Debrief
What stops the review job from running away: turns, time, money?
