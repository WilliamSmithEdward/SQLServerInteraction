# Notes for agents

<!-- repo-standards:begin. Copied from WilliamSmithEdward/repo-standards, templates/agents/AGENTS-block.md. Change it there; the weekly rescan fails a copy that differs. -->
## Releases, CI and security

These rules are the same in every WilliamSmithEdward repository.

- **How a release happens here:** pushing a `vX.Y.Z` tag runs Publish, which builds the release files in CI and creates the GitHub release with them, their signed provenance and the security reports. Any other step, such as a marketplace upload, is described elsewhere in this file.
- **Starting a workflow by hand never releases anything.** Publish and every
  release report are dry runs when started with `gh workflow run` or the Run
  workflow button. They build, scan and assemble the release files exactly
  as a release would, and upload them as the `release-preview` artifact
  instead. Run one after changing anything on the release path:
  `gh workflow run <file> --ref main`, then
  `gh run download <run-id> -n release-preview`.
- **Do not create, publish, edit or delete a release or a `v*` tag** unless
  the owner asks for it. A `v*` tag cannot be moved or deleted once pushed.
- **Every change to `main` goes through a pull request** that passes CI
  passed, Security passed and Malware scan passed. No one can push to `main`
  directly or skip the checks, admins included. Push a branch, open a pull
  request, and let it merge itself: `gh pr merge --auto --squash <number>`.
- **Pins.** Actions by full commit SHA with the version as a comment. Images
  by digest, in `.github/security/<tool>/Dockerfile`. Python tools from the
  hash-locked `.github/requirements/<purpose>.txt`, compiled from the `.in`
  beside it with
  `uv pip compile <purpose>.in --universal --generate-hashes --python-version 3.12 -o <purpose>.txt`.
  Runners are named releases, never `-latest`.
- **Updates merge themselves.** Dependabot and the Update YARA rules workflow
  open pull requests that merge once the three checks pass, except a
  third-party major version, which waits for the owner. Leave them alone
  unless asked.
- **A scanner finding is fixed or accepted with a written reason** in the
  repository's accepted list. Never silence a scanner without one.
<!-- repo-standards:end -->

## This repository

SQLServerInteraction is a .NET library of helper methods over
Microsoft.Data.SqlClient, published to nuget.org as `SQLServerInteraction`
for net9.0. What an agent working here must not break:

- **The release path.** A release starts from a `vX.Y.Z` tag that matches
  `PackageVersion` in `SQLServerInteraction/SQLServerInteraction.csproj`
  (keep `AssemblyVersion` the same); Publish refuses any other. Its notes are
  the version's section of `CHANGELOG.md` (`## [X.Y.Z] - date`), written
  before the tag is pushed; without one the release fails. The package goes
  to nuget.org through trusted publishing: nuget.org's policy is bound to
  `publish.yml` and the `nuget` environment, so both keep their names, and no
  API key is stored anywhere.
- **Names are quoted, SQL is not.** Every table, column, index and database
  name a caller passes goes through `SqlIdentifier.Quote` or is sent as a
  parameter; values are always parameters. Only the `sql` strings, the
  conditions of UpdateData, DeleteData and BulkCopy, and QueryBuilder's
  strings are SQL by design. Each Semgrep `csharp-sqli` finding that remains
  is accepted in `.github/security/accepted.toml` with its reason, and the
  ones at quoted-identifier sites name the integration test that runs them on
  a hostile name. A new SQL-building site needs the same, or a fix.
- **Public signatures.** Callers compile against them: add an overload or a
  method instead of a parameter on an existing public method, even an
  optional one.
- **Two READMEs that say the same things.** `README.md` is the GitHub page and
  `SQLServerInteraction/nugetREADME.md` is packed as the nuget.org readme.
  Change both in the same pull request. They differ only in the badge block,
  which `nugetREADME.md` leaves out because nuget.org does not render images
  from api.scorecard.dev. Links in both are absolute.
- **The lock files.** Restores run with `--locked-mode` against
  `SQLServerInteraction/packages.lock.json` and
  `SQLServerInteraction.Tests/packages.lock.json`. A changed package reference
  is restored once with `dotnet restore --force-evaluate`, and both updated
  lock files are committed with it; a library update that leaves the test
  project's lock file behind fails CI.
- **Tests.** `SQLServerInteraction.Tests` is an xUnit v3 project run by
  Microsoft.Testing.Platform (`global.json` opts `dotnet test` in). Unit tests
  need nothing. The integration tests need SQL Server and are skipped, not
  passed, without one. `scripts/test/run-integration-tests.sh` builds the image
  pinned in `.github/test/mssql/Dockerfile`, starts it on 127.0.0.1 with an
  SA password made for the run, waits for it, runs `dotnet test` with any
  arguments you give it, and removes the container; CI runs it with
  `-c Release --no-build --fail-skips on`, so a skipped test fails there. The
  tests connect to nothing but that container. A fix comes with a test that
  fails without it.
