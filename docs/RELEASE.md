# GameCore 1.0 release checklist

Implementation for phases 0–13 is present in the repository. A release tag must only be created after the local verification pass succeeds.

## Verification

1. Reset and create `GameCoreDB`.
2. Run `database/setup.sql` successfully.
3. Build the .NET solution in Release.
4. Run `dotnet list src/backend/GameCore.sln package --vulnerable --include-transitive`.
5. Run `npm install`, `npm run build`, and `npm audit --audit-level=high`.
6. Start API and frontend.
7. Run `scripts/smoke-test.ps1`.
8. Verify login, dashboard, games, customers, sales, employees and distribution.
9. Replace demo credentials/JWT signing key before a non-local deployment.

## Release

After all checks pass, tag the verified commit as `v1.0.0`.
