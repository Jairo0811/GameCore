# GameCore 1.0.1 release process

> `v1.0.0` (2026-09-28) was the first restored release. `v1.0.1` is the frozen release produced by the final audit
> (RBAC, validation, Docker, tests, CI); it is the version this checklist verifies.

`v1.0.1` is tagged only on a commit for which **every** item below has passed. If anything fails, fix it on the release branch and start the checklist again.

## 1. Continuous integration (GitHub Actions — `.github/workflows/ci.yml`)

Runs on pushes to `main` and `release/**` and on pull requests.

| Job | Verifies |
|---|---|
| `backend` | PowerShell scripts parse; `dotnet restore`; Release build; `dotnet test` (unit + API over SQLite); NuGet vulnerability audit (fails if any package is reported) |
| `frontend` | `npm ci`; production build; `npm test`; `npm audit --audit-level=high` |
| `full-stack` | `docker compose config`; `docker compose up -d --build` **from scratch**; API `/health` (includes GameCoreDB); frontend served; `scripts/smoke-test.ps1`; SQL Server integration tests; re-running `db-init` keeps the existing database |

All three jobs must be green on the commit to be tagged. Besides the checks above, `full-stack` also runs
`setup-db.ps1 -Reset` (drop + full create while the API is connected), `setup-db.ps1` (validate only),
the complete `release-check.ps1 -IncludeSqlServerTests` against the Docker SQL Server, and the upgrade path
of `database/upgrade-v1.sql` on a simulated v1.0.0 database.

## 2. Local verification (Windows + SQL Server)

From the repository root:

```powershell
# Full check on a fresh database (drops GameCoreDB after confirmation):
.\scripts\release-check.ps1 -ResetDatabase -IncludeSqlServerTests

# Or, keeping your existing GameCoreDB (validated, never modified):
.\scripts\release-check.ps1
```

`release-check.ps1` performs, in order:

1. **Database**: `setup-db.ps1` creates `GameCoreDB` with the official UTF-8 bootstrap (`sqlcmd -f 65001 -i database/setup.sql`) if it does not exist; otherwise it runs `database/validate.sql` and `database/advanced/validate.sql`. Validation covers core tables, seed data, Unicode text, demo users for the three roles, the filtered unique email indexes (several NULL emails allowed, duplicate values rejected) and the advanced SQL objects. Nothing is deleted unless `-ResetDatabase` is given.
2. **Backend**: restore, Release build, tests, NuGet vulnerability audit.
3. **Frontend**: `npm ci`, production build, tests, `npm audit --audit-level=high`.
4. **API**: reuses an API already running at `-ApiUrl` (default `http://localhost:5152`) or starts the Release build and stops it afterwards.
5. **Smoke test** (read-only): health with database, login for Admin/Manager/Viewer, invalid login, anonymous access, Dashboard, Reports, Games, Customers, Sales, Employees, Distributions, Catalogs, Viewer write protection (403) and pre-database validation (duplicated `GameId` in a sale returns 400).
6. **SQL Server integration tests** (`-IncludeSqlServerTests`): run against the real database and create demo rows.

It ends with `GAMECORE v1.0.1 RELEASE CHECK PASSED`.

## 3. Manual UI pass

With API and frontend running, sign in as each demo account (password `GameCore123!`):

- `admin@gamecore.local` / `manager@gamecore.local`: create and edit a game (genres/platforms), customer, employee and distribution (edit must update the same row), create a sale, check Dashboard and Reports.
- `viewer@gamecore.local`: all modules readable; no create/edit buttons.
- Let a session expire or clear the token: the app returns to the login with a "session expired" message.

## 4. Before any non-local deployment

Replace the demo accounts and set a real `Jwt__Key` (≥ 32 bytes, not starting with `CHANGE-ME`). The API refuses to start in `Production` with the development key. `docker-compose.yml` is a local development stack only.

## 5. Tag

```powershell
git tag -a v1.0.1 -m "GameCore v1.0.1"
git push origin v1.0.1
```

After tagging, the project is frozen: only critical bug fixes or a planned future version.
