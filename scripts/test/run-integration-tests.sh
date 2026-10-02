#!/usr/bin/env bash
# Runs the whole test suite, unit and integration, against a throwaway SQL
# Server container built from .github/test/mssql/Dockerfile. CI runs this,
# and so can anyone with Docker and the .NET SDK (Git Bash on Windows too):
#
#   scripts/test/run-integration-tests.sh [dotnet test arguments...]
#
# The SA password is generated here for this run and lives only in this
# process's environment: it is never written to a file, printed or put on a
# command line. The container listens on 127.0.0.1 only, on
# SQLSERVERINTERACTION_TEST_PORT (14333 by default), and is removed when the
# script exits, whatever the outcome.
set -euo pipefail

# Git Bash would rewrite the paths inside the container below.
export MSYS_NO_PATHCONV=1

cd "$(dirname "$0")/../.."
image=sqlserverinteraction-test-mssql
container=sqlserverinteraction-test-$$
export SQLSERVERINTERACTION_TEST_PORT=${SQLSERVERINTERACTION_TEST_PORT:-14333}

# Upper case, lower case and digits, which SQL Server's password policy needs.
password="Aa1$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 32 || true)"
if [ "${#password}" -ne 35 ]; then
  echo "could not generate a password" >&2
  exit 1
fi
if [ -n "${GITHUB_ACTIONS:-}" ]; then
  echo "::add-mask::$password"
fi
# The tests, SQL Server and sqlcmd each read it from their environment.
export SQLSERVERINTERACTION_TEST_SA_PASSWORD="$password"
export MSSQL_SA_PASSWORD="$password"
export SQLCMDPASSWORD="$password"
export ACCEPT_EULA=Y

cleanup() {
  docker rm --force "$container" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker build --quiet --tag "$image" .github/test/mssql >/dev/null
docker run --detach --name "$container" \
  --env ACCEPT_EULA --env MSSQL_SA_PASSWORD \
  --publish "127.0.0.1:$SQLSERVERINTERACTION_TEST_PORT:1433" \
  "$image" >/dev/null

echo "Waiting for SQL Server to accept connections..."
deadline=$((SECONDS + 180))
until docker exec --env SQLCMDPASSWORD "$container" \
  /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U sa -C -l 5 -b -Q "SELECT 1" >/dev/null 2>&1; do
  if [ "$SECONDS" -ge "$deadline" ]; then
    echo "SQL Server did not accept connections within 180 seconds. Its log:" >&2
    docker logs "$container" 2>&1 | tail -n 40 >&2
    exit 1
  fi
  sleep 3
done
echo "SQL Server is ready."

dotnet test SQLServerInteraction.sln "$@"
