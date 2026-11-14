# GameCore Frontend

React + TypeScript + Vite administrative interface for GameCore.

## Current toolchain

- React 19.3
- TypeScript 7.0
- Vite 8.3

## Run

```powershell
cd src/frontend
npm ci
npm run dev
```

## Test and build

```powershell
npm test -- --run      # Vitest: session storage, API client, 401/403 handling
npm run build          # type check + production bundle
npm audit --audit-level=high
```

The default API URL is `http://localhost:5152`. Override it with `VITE_API_URL` when necessary.

## Demo login

Password for all demo accounts: `GameCore123!` — `admin@gamecore.local` (Admin), `manager@gamecore.local` (Manager), `viewer@gamecore.local` (Viewer, read-only UI).

The login form is pre-filled with the Admin account only in development (`npm run dev`); production builds start empty.

## Session

The session lives in `sessionStorage`. Corrupt or expired values are discarded on load, and any HTTP 401 from the API clears the session and returns to the login with a "session expired" message.

These credentials and the repository JWT key are strictly local demo defaults. Replace them before any non-local deployment.
