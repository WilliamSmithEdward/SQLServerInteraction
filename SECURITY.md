# Security policy

## Reporting a vulnerability

Report a vulnerability privately, not in a public issue or pull request:
[open a private report](https://github.com/WilliamSmithEdward/SQLServerInteraction/security/advisories/new).
Only the maintainer sees it. Include the SQLServerInteraction version, the
.NET and SQL Server versions, the method involved, and the smallest code
that shows it, with server names, credentials and private data removed.

A confirmed vulnerability is fixed in a release on nuget.org, and the
advisory is published with it, crediting you unless you ask otherwise.

## Supported versions

Only the latest release on nuget.org receives security fixes. Older
releases are not maintained separately; update when a fix ships.

## Scope

SQLServerInteraction is a library. It opens no port and starts no process.
Each method opens a connection to the SQL Server the caller's connection
string names, through Microsoft.Data.SqlClient, runs one piece of work, and
closes it. `ExecuteScriptFromFileAsync` reads the script file the caller
names, `ExportDataToCSVAsync` writes the CSV file the caller names, and
`BackupDatabase` and `RestoreDatabase` ask the server to write or read a
backup at a path on the server.

The library sends three kinds of caller input to the server: values, names
and SQL. Values (parameter dictionaries and `SqlParameter` arrays) are sent
as parameters. Names of tables, columns, indexes and databases are quoted as
identifiers or sent as parameters, and backup paths are sent as parameters.
Some strings are SQL by design: the `sql` strings of the query and command
methods, the conditions of `UpdateData`, `DeleteData` and `BulkCopy`, and
every string `QueryBuilder` takes. These count as vulnerabilities:

- a name or a path the library takes that changes the SQL it runs, rather
  than being read as a name or a path;
- a value that reaches the server as SQL text rather than as a parameter;
- a value in `SQLServerConnectionString` (a server name, a password) that
  adds or changes a connection string keyword;
- a value or name that makes the library read or write a file, or reach a
  server, other than the ones the caller named.

SQL that runs because the caller passed it as SQL is not a vulnerability in
the library. A wrong result or an exception is not one on its own; report it
as an issue.

### SQL you write

Build the `sql` strings, the conditions and the QueryBuilder strings in your
own code. Put any value that comes from a user, a file or another system in
the parameters: every method that takes a condition has an overload that
takes parameters for it, and `QueryBuilder.Build` returns the values
`AddParameter` recorded. Never concatenate such a value into the SQL text.

### Connection strings and credentials

Keep server names, user names and passwords out of source code and logs.
Read them from configuration or a secret store. Encryption is on by default
(`Encrypt=True`); turn it off, or trust the server certificate, only for a
server you control on a network you trust.

## How the code is checked

Three workflows check every pull request and every push to `main`, and
their gates decide whether a change can merge: **CI passed**,
**Security passed** and **Malware scan passed**. A gate passes only when
every job before it did, and any unexpected finding fails it, whatever its
severity. CI runs the unit tests and the integration tests against SQL
Server 2022 in a container, among them tests that pass hostile names through
every method that takes a name, and fails if any test was skipped. Security
also runs weekly, so new queries, rules and advisories reach code that has
not changed, and Malware scan runs daily, so new signatures and rules reach
files that have not changed.

- **Code:** CodeQL with GitHub's security-extended queries, for C#
  (extracted from a Release build of the library and its tests) and GitHub
  Actions, and Semgrep with the default, C#, security-audit, secrets and
  GitHub Actions rule sets. Semgrep scans the library, the workflows that
  build and publish it, and the scripts in `scripts/security` that judge the
  scans. A `nosemgrep` comment cannot hide a finding. Results go to the
  repository's code scanning.
- **Workflows:** zizmor audits the GitHub Actions workflows; a finding fails
  Security.
- **Dependencies:** `dotnet list package --vulnerable --include-transitive`
  checks the packages `packages.lock.json` resolves, after the same locked
  restore CI builds with, and any known vulnerability fails Security. The
  library depends on Microsoft.Data.SqlClient and Azure.Identity and what
  they bring in.
- **Malware:** ClamAV, with signatures freshclam fetches and verifies on
  every run, and YARA-X, with the YARA Forge rules pinned to a release and
  its SHA-256, scan every file the commit holds and the .nupkg built from it
  with the locked restore, both as the archive and unpacked. On a release
  they scan the very .nupkg that is published. YARA-X runs YARA Forge's full
  rule set. A scan error fails the report as a match does.
- **OpenSSF Scorecard** rates the repository's security practices on every
  change to `main` and weekly, and the README badge shows the result.
  Its Code-Review and Contributors checks assume more than one
  maintainer, such as a second person approving every change, so a
  single-maintainer project cannot score full marks on them. Its Fuzzing
  check finds no C# fuzzer short of OSS-Fuzz or ClusterFuzzLite, so this
  repository has no fuzz workflow; the library parses no untrusted input
  itself.

## Accepted findings

A finding is fixed, or accepted with a written reason in
[.github/security/accepted.toml](https://github.com/WilliamSmithEdward/SQLServerInteraction/blob/main/.github/security/accepted.toml)
for CodeQL and Semgrep, or
[.github/security/malware-accepted.toml](https://github.com/WilliamSmithEdward/SQLServerInteraction/blob/main/.github/security/malware-accepted.toml)
for ClamAV and YARA-X. A finding entry matches on the tool, the rule and
the file, and in accepted.toml also the text of the flagged line, so an
edited line needs another review; a notice entry, for a warning a tool
raises about its own scan, matches on the tool, the warning and text the
message contains. An entry that no longer matches fails the report. zizmor
keeps its exceptions in `.github/zizmor.yml` or inline beside the line they
excuse, each with its reason.

The current entries: accepted.toml accepts 31 Semgrep `csharp-sqli`
findings, one entry per flagged line, because the rule flags every
`SqlCommand` built from a string that is not a literal. 19 are in methods
whose purpose is to run the caller's SQL, or that send a stored procedure
name with `CommandType.StoredProcedure`; 6 are at the conditions of
`UpdateData`, `DeleteData` and `BulkCopy`, SQL by design; and 6 are at
`GetTableRowCount`, `GetTableSchema`, `InsertData` and `InsertData<T>`,
whose only caller strings are names quoted by `SqlIdentifier.Quote`, each
entry naming the integration test that runs the method on a hostile name.
There are none in malware-accepted.toml. zizmor's `self-repository` and
`superfluous-actions` rules are turned off in `.github/zizmor.yml`, each
with its reason and when it comes back.

## Pinning and updates

Everything the workflows run is pinned: actions to full commit SHAs,
runners to named OS releases, scanner images and the SQL Server image the
tests run against to digests, Python tools to hash-locked lock files, the
library's and the tests' NuGet packages to their `packages.lock.json`,
restored in locked mode, the YARA Forge rules to a release and its SHA-256,
and the YARA-X engine to a release and its SHA-256. `global.json` sets the
.NET SDK's floor at 10.0.400 and lets it roll forward to a newer feature
band, so CI builds with the newest .NET 10 SDK; the test job also installs
the .NET 9 SDK named in `ci.yml`, an exact release moved by hand, for the
.NET 9 runtime the tests run on. ClamAV's signatures change too often to
pin, so freshclam fetches and verifies them on every run.

Dependabot proposes updates to the GitHub Actions, the Semgrep, ClamAV and
SQL Server images, the hash-locked files in `.github/requirements`, the
NuGet packages and the .NET SDK in `global.json` once a version is a week
old, and at once for a security advisory. The Update YARA rules workflow
proposes new YARA pins in `.github/security/yara.json` each week. A minor
or patch update, and the YARA pull request, merges itself once CI, Security
and Malware scan pass; a third-party major version waits for review.

## Releases

A pushed `vX.Y.Z` tag builds the .nupkg with the locked restore, checks
that the tag matches the package version (`PackageVersion` in the csproj),
and runs Security and Malware scan on the tagged commit and that package.
Nothing is published unless all of them pass. The package goes to
nuget.org through trusted publishing, so no long-lived API key exists to
leak. The GitHub release, titled with the tag, carries the .nupkg,
`SQLServerInteraction-<version>-security-report.md` and
`SQLServerInteraction-<version>-malware-report.md` beside the scan results
they were made from, and the provenance bundle, with the version's section
of `CHANGELOG.md` as its notes. Started by hand, the Publish workflow is
always a dry run and publishes nothing.

### Verifying a download

Releases from 2.0.0 carry a GitHub build provenance attestation for the
.nupkg CI built. Check the copy attached to the GitHub release:

```
gh attestation verify SQLServerInteraction.<version>.nupkg --owner WilliamSmithEdward
```

The output names the commit and workflow run that built the file. The
signed bundle is also attached to the release as
`SQLServerInteraction-<version>.sigstore.json`, so the check works without
asking GitHub for it: add
`--bundle SQLServerInteraction-<version>.sigstore.json`.

nuget.org adds its own repository signature to every package it serves,
which changes the file, so the copy from nuget.org does not match the
attestation. Check that copy's signature with `dotnet nuget verify`.

## Repository settings

<!-- repo-standards:begin security-settings. Copied from WilliamSmithEdward/repo-standards, templates/security/settings-block.md. Change it there; the weekly rescan fails a copy that differs. -->
- `main` accepts changes only through a pull request that passes
  **CI passed**, **Security passed** and **Malware scan passed**. The
  ruleset has no bypass, for the owner either, and refuses force-pushes and
  deleting the branch.
- A `v*` release tag cannot be moved or deleted once pushed, except by a
  repository admin.
- A workflow that uses an action not pinned to a full commit SHA fails to
  run. Workflow tokens are read-only unless a job is granted more for
  itself.
- Secret scanning with push protection, Dependabot alerts and security
  updates, and private vulnerability reporting are on.
<!-- repo-standards:end -->
