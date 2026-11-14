#!/usr/bin/env bash
# One-shot GameCoreDB bootstrap for docker compose (service "db-init").
#
# - GameCoreDB missing  -> runs database/setup.sql (schema, seed, AppUsers, advanced SQL, validations).
# - GameCoreDB present  -> never modifies it; only re-runs the validation scripts.
#   If validation fails (old schema or a half-finished bootstrap) the service exits with an error;
#   recreate the local volume with:  docker compose down -v && docker compose up --build
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
[ -x "$SQLCMD" ] || SQLCMD=/opt/mssql-tools/bin/sqlcmd
SERVER="${SQL_SERVER_HOST:-sqlserver}"
: "${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"

run_sql() {
  "$SQLCMD" -S "$SERVER" -U sa -P "$MSSQL_SA_PASSWORD" -C -I -b -f 65001 "$@"
}

echo "[db-init] Waiting for SQL Server at ${SERVER}..."
for attempt in $(seq 1 60); do
  if run_sql -Q "SELECT 1" >/dev/null 2>&1; then break; fi
  if [ "$attempt" -eq 60 ]; then echo "[db-init] SQL Server did not become available." >&2; exit 1; fi
  sleep 2
done

cd /gamecore
state="$(run_sql -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'GameCoreDB') IS NULL THEN 'missing' ELSE 'present' END" | tr -d '[:space:]')"

if [ "$state" = "missing" ]; then
  echo "[db-init] GameCoreDB not found. Running database/setup.sql..."
  # setup.sql uses Windows-style ':r .\database\...' includes; normalise them for Linux sqlcmd.
  sed 's#\\#/#g' database/setup.sql > /tmp/gamecore-setup.sql
  run_sql -i /tmp/gamecore-setup.sql
  echo "[db-init] GameCoreDB created and validated."
else
  echo "[db-init] GameCoreDB already exists. Validating without changes..."
  if ! run_sql -i database/validate.sql || ! run_sql -i database/advanced/validate.sql; then
    echo "[db-init] The existing GameCoreDB is not valid for this version." >&2
    echo "[db-init] Recreate the local Docker database with: docker compose down -v && docker compose up --build" >&2
    exit 1
  fi
  echo "[db-init] Existing GameCoreDB is valid."
fi
