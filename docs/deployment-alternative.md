# Alternative deployment: Vercel + Neon (if Render is down)

Use this only when the trainer says so. The labs are written for Render; this page changes **Lab 4 step 4.2–4.5** (database) and **Lab 5 steps 5.2–5.3 and 5.7** (hosting and deploy). Everything else stays the same, including the CI gates.

| Part | Render (normal) | Fallback |
|---|---|---|
| Database | Render Postgres (Lab 4) | **Neon** Postgres, Singapore (`aws-ap-southeast-1`), created from Vercel |
| API + web app | Render Docker service from `render.yaml` | **Vercel** Container runtime from `Dockerfile.vercel` (repo root, from `lab5-start` on) |
| Deploy after CI | `deploy.yml` calls `RENDER_DEPLOY_HOOK` | Vercel deploys `main` itself when it changes. `main` is protected, so only merged pull requests that passed `ci` and `claude-review` get there |

**Yes, it runs end to end:** the same .NET API serves the web app at `/app`, in one container, against Postgres. Tested on 15 Nov 2026 with the app on `Dockerfile.vercel`'s runtime image (`aspnet:8.0-alpine`, 231 MB), a read-only file system (as on Vercel) and Postgres 16: `/health` showed the Npgsql provider, `/app/` loaded, sign-in with the private `SEED_USER_PASSWORD` worked and `Acme@2026` was refused. The Vercel build and deploy themselves have not been run yet: do one trial deploy before the workshop.

## Know before you choose it
- Vercel's Container runtime is **Beta** (all plans). Use a personal **Hobby** account.
- **You get signed out after about 5 minutes idle.** Vercel stops an idle instance after 5 minutes, and the app's sign-in tokens do not survive a restart (the keys live in memory). Sign in again. The same happens on Render's free plan after 15 minutes asleep.
- **Request size limit 4.5 MB.** Lab 6 sample PDFs are far smaller. A PDF between 4.5 and 5 MB gets Vercel's 413 instead of the app's 400.
- **Request time limit 300 s** on Hobby. Policy research and document extraction finish well inside it.
- **No local disk.** The SQLite fallback cannot run on Vercel; `DATABASE_URL` must be set.
- **The Render MCP steps (Lab 4 steps 4.1, 4.2, 4.6) cannot be done** on this path. Watch the trainer's demo instead, or do them later.

## A. Database (replaces Lab 4 steps 4.2–4.4)
1. Sign in to vercel.com with GitHub. **Add New → Project →** import your repo. If Vercel starts a first deploy, let it fail: the settings below come first.
2. Project → **Storage → Create Database → Neon (Postgres)**. Region **Singapore**, free plan. Connect it to the project for **Production** (and **Development** if offered).
3. Project → **Settings → Environment Variables**: check that `DATABASE_URL` is listed. If it is not, copy the connection string from the Neon tab into a new `DATABASE_URL` yourself.
4. For local work (Lab 4): copy the same connection string into `.env` as `DATABASE_URL`, in your editor, never in chat. Neon needs no IP allow-list step. Set `SEED_USER_PASSWORD` in `.env` to your private password, as in step 4.4.
5. Restart the API. `/health` must show `"provider":"Npgsql.EntityFrameworkCore.PostgreSQL"`.

## B. Hosting (replaces Lab 5 steps 5.2–5.3)
1. Project → **Settings → Environment Variables**, environment **Production**. Add by hand, never through Claude:
   - `PORT` = `8080` (the .NET image listens on 8080; Vercel's default is 80)
   - `ANTHROPIC_API_KEY` = your key with a spend limit
   - `SEED_USER_PASSWORD` = the same private password as in your `.env`
   - `DATABASE_URL` is already there from step A
2. Project → **Settings → Functions → Function Region**: **Singapore (sin1)**, next to the database.
3. `Dockerfile.vercel` at the repo root makes Vercel use its Container preset. If the project shows another framework, run `npx vercel@latest project update --framework container --yes` in the repo folder.
4. Project → **Settings → Git → Production Branch**: `main`.
5. GitHub → your repo → **Settings → Secrets and variables → Actions**: delete `RENDER_DEPLOY_HOOK` if you added it. `deploy.yml` then prints "skipping API deploy" and stays green; Vercel deploys instead.
6. **Deployments → Redeploy** the latest `main` once (or push to `main`). The first build takes several minutes: it builds the web app, the .NET API and the image.
7. Open `https://<project>.vercel.app/health`, then `/app/`, and sign in as Farah with your private password.

Vercel also builds a **Preview** for every pushed branch. That is fine for the labs (each preview uses the same database); you can switch previews off in Settings → Git.

## C. Check the deploy (replaces Lab 5 step 5.7)
After you merge the Lab 5 fix: Vercel → **Deployments** shows a **Production** deployment for the merge commit, status **Ready**. Then open `/health` and sign in.

## Lab 6 on this path
Unchanged. On the live site use `https://<project>.vercel.app/app/documents/` and `/app/review/`.

## After the workshop
Delete the Vercel project and the Neon database, and delete or limit the Anthropic key you gave to Vercel and GitHub.
