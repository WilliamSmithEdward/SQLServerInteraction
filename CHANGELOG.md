# Changelog

Each release's notes. The Publish workflow takes the section for the
version it releases as the GitHub release's body, so a section is written
here before the version is tagged.

The sections up to 1.1.1 were gathered from the nuget.org version history,
with the date nuget.org records for each upload. Neither nuget.org nor the
repository carried release notes for them. Versions 1.0.0 to 1.0.18 are
unlisted on nuget.org.

## [2.0.0] - 2026-10-02

Table, column, index and database names are now quoted as identifiers, and backup paths, lookups and condition values are sent as parameters, so a name or path that came from outside can no longer change the SQL the library runs. Several fixes change what callers see, hence the major version.

### Breaking changes

* An empty or whitespace condition in `DeleteData`, `UpdateData` and their async versions, and in `BulkCopy`'s `flushWhereClauseCondition`, throws `ArgumentException` instead of affecting every row. `DeleteData(table)` with no condition now throws too. Pass `"1 = 1"` to mean every row. A null flush condition still deletes every row when `flushTable` is set.
* `GetDatabaseSizeInBytes` returns bytes. It returned kilobytes, so its result is 1024 times larger than before.
* The `SortOrder` enum that `QueryBuilder.OrderBy` takes is renamed `QuerySortOrder`, so it no longer clashes with `Microsoft.Data.SqlClient.SortOrder`.
* A table name is read as a one-, two- or three-part name and quoted, so text that only worked because it was pasted into the SQL, such as an alias or a table hint, no longer does. A malformed name (an empty part, an unclosed bracket, more than three parts, a part over 128 characters) throws `ArgumentException`. A dictionary key, `IndexCreate`'s column and `IndexDrop`'s index are one name each; bracket one that contains a dot.
* `InsertData` and `UpdateData` send their values as parameters named `@__value_0`, `@__value_1` and so on, not as parameters named after the keys, and an empty values dictionary throws `ArgumentException`.
* `InsertData<T>` writes a property to the column its `SQLServerInstance.Column` attribute names, and leaves out static properties and indexers. It used the property name and took every public property.
* `DoesTableExist`, `GetTableColumns`, `GetColumnNames` and `GetTablePrimaryKeyColumn` read `dbo.Orders` as schema `dbo`, table `Orders`; they used to look for a table named `dbo.Orders` and find none. A bare name still matches in any schema.
* `SQLServerConnectionString.GetConnectionString()` builds the string with `SqlConnectionStringBuilder`, so its text uses SqlClient's keywords (`Data Source`, `Initial Catalog`, `Integrated Security`, `User ID`) and quotes values that need it. Additional parameters that are not a valid connection string throw `ArgumentException`. Their keywords still replace the ones the other arguments set.
* In `QueryBuilder`, `Paginate` without an `OrderBy` throws at `Build()`, a page or page size below 1 throws, ending a nested condition that holds nothing throws, and `Build()` needs a `FROM` outside parentheses, quotes and comments. `Union`, `Intersect` and `Except` are obsolete: they only put their keyword in front of the builder's own SQL.
* The package depends on Microsoft.Data.SqlClient 7.1.0, up from 5.2.

### Fixes

* `BackupDatabase` and `RestoreDatabase` send the database name and the path as parameters, so a database name with `]` or a path with `'` works. `RestoreDatabase` switches its connection to `master` before restoring.
* The table lookups send the name as parameters instead of placing it between quotes in the SQL.
* `ExecuteScalar<T>` with a nullable type such as `int?` converts the value instead of throwing.
* Every method that takes a parameter dictionary adds the `@` only when a name lacks it; `ExecuteNonQueryWithParameters` used to turn `@Name` into `@@Name`.
* A null value in a parameter or values dictionary is sent as SQL NULL in `InsertData`, `UpdateData` and `ExecuteNonQueryWithParameters`, instead of being dropped.
* `GetTablePrimaryKeyColumn` returns the first key column in key order instead of an arbitrary one.
* A `;`, `=` or quote in a connection string value, such as a password, no longer breaks the string, and the stray `;;` is gone.
* `QueryBuilder.OrderBy` writes `ASC` and `DESC`, so `Paginate` works. The aggregates and CASE expressions join the SELECT list with commas, `StartCaseStatement("")` builds a searched CASE, nested conditions are wrapped in parentheses, a subquery's SQL and parameters are embedded where it was created, and `Build()` leaves the builder unchanged, so a second call returns the same SQL.

### Additions

* `DeleteData`, `UpdateData` and `BulkCopy`, and their async versions, have overloads that take parameters for the condition.
* `BulkCopy`'s new overload also takes `columnMappings`, which copies DataTable columns to named destination columns instead of by position.
* `GetTablePrimaryKeyColumns` returns every primary key column in key order.
* `QueryBuildResult.ParameterValues` holds the values `AddParameter` recorded, ready to pass with the SQL to any method that takes a parameter dictionary.
* The repository has unit tests and integration tests that run against SQL Server 2022 in CI. The package is built in CI from the tagged commit, scanned for vulnerabilities and malware, and published through nuget.org trusted publishing, and the GitHub release carries its signed build provenance.

## [1.1.1] - 2026-05-20

No notes were recorded.

## [1.1.0] - 2026-05-20

No notes were recorded.

## [1.0.20] - 2025-03-17

No notes were recorded.

## [1.0.19] - 2024-06-27

No notes were recorded.

## [1.0.18] - 2024-02-02

No notes were recorded.

## [1.0.17] - 2024-01-17

No notes were recorded.

## [1.0.16.2] - 2023-09-30

No notes were recorded.

## [1.0.16.1] - 2023-09-30

No notes were recorded.

## [1.0.16] - 2023-09-28

No notes were recorded.

## [1.0.15] - 2023-09-26

No notes were recorded.

## [1.0.14] - 2023-09-26

No notes were recorded.

## [1.0.13] - 2023-09-26

No notes were recorded.

## [1.0.12] - 2023-09-25

No notes were recorded.

## [1.0.11] - 2023-09-25

No notes were recorded.

## [1.0.10] - 2023-09-25

No notes were recorded.

## [1.0.9] - 2023-09-22

No notes were recorded.

## [1.0.8] - 2023-09-22

No notes were recorded.

## [1.0.7] - 2023-09-22

No notes were recorded.

## [1.0.6] - 2023-09-21

No notes were recorded.

## [1.0.5] - 2023-09-20

No notes were recorded.

## [1.0.4] - 2023-09-20

No notes were recorded.

## [1.0.3] - 2023-09-20

No notes were recorded.

## [1.0.2] - 2023-09-20

No notes were recorded.

## [1.0.1] - 2023-09-20

No notes were recorded.

## [1.0.0] - 2023-09-20

No notes were recorded.
