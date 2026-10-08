# SQLServerInteraction: primer

A reference for anyone, human or agent, writing code against this library or
changing it. It states what every public member does, the rules that hold
across all of them, and every breaking change from 1.x to 3.0.0. The
`README.md` has the worked examples; this file has the contract.

Package: `SQLServerInteraction` on nuget.org, `net9.0`, namespace
`SQLServerInteraction`, over `Microsoft.Data.SqlClient`.

## 1. The model

- `SQLServerInstance` holds a connection string. Each of its methods opens a
  connection, does one piece of work, and closes the connection. Nothing is
  cached between calls, and the object has no state besides the string.
- `SQLServerTransaction`, from `BeginTransactionAsync()`, holds one open connection
  and one transaction. The data methods on it run in that transaction until
  `CommitAsync()`. Disposing it without a commit rolls back.
- `SQLServerConnectionString` builds a connection string.
- `QueryBuilder` assembles a SELECT statement as text. It never connects.
- Errors are not caught. A server error is SqlClient's `SqlException`; a
  refused argument is `ArgumentException`, thrown before anything runs.
- Commands use SqlClient's default 30-second timeout; only `BulkCopyAsync` and
  `BulkMergeAsync` take a timeout. Every method is async and takes an optional
  `CancellationToken` as its last argument.

## 2. Names are quoted, SQL is not

This is the one rule to hold in mind for every call.

**A name is read as a name.** Every table, column, index and database name a
method takes goes through `SqlIdentifier`: a one-, two- or three-part name,
each part plain or in brackets (`Sales`, `dbo.Sales`, `[dbo].[My Sales]`,
`MyDb.dbo.Sales`), is split on dots outside brackets and each part is wrapped
in brackets with any `]` doubled. A column or index name is one part; bracket
one that contains a dot. A malformed name throws `ArgumentException`: an empty
part, an unclosed bracket, text after a closing bracket, more than the allowed
parts, or a part over 128 characters. Lookups that compare with the catalog
send the parts as parameters instead. A hostile name such as
`x]; DROP TABLE Victim; --` is therefore just an odd table name, and the
integration tests prove it on a real server.

**A value is a parameter.** Dictionary values and `SqlParameter` arrays go to
the server as parameters. Null and `DBNull.Value` are sent as SQL NULL.
Parameter names work with or without the `@`. `InsertDataAsync` and `UpdateDataAsync`
name their value parameters `@__value_0`, `@__value_1`, ..., so do not use
those names for condition parameters.

**These strings are SQL, run as written.** Build them in code; never from
user input.

| Method | SQL text |
|---|---|
| `ExecuteQueryAsync`, `ExecuteQueryAsync<T>`, `ExecuteScalarAsync<T>`, `ExecuteSQLAsync`, `ExecuteTransactionAsync`, `ExportDataToCSVAsync`, `ExecuteScriptFromFileAsync` | the whole statement (or file) |
| `ExecuteNonQueryWithParametersAsync`, `ExecuteParameterizedQueryAsync`, `ExecuteQueryToObjectListAsync<T>` | the whole statement; values are parameters |
| `UpdateDataAsync`, `DeleteDataAsync` | the condition after `WHERE`; values can be parameters |
| `BulkCopyAsync` | `flushWhereClauseCondition`; values can be `flushParameters` |
| `QueryBuilder` | every string, table names included |

`MergeDataAsync`, `BulkMergeAsync`, `InsertDataAsync`, the index methods and the schema
lookups run no caller-written SQL at all.

**An empty condition is refused.** `UpdateDataAsync`, `DeleteDataAsync` and a non-null
`flushWhereClauseCondition` throw `ArgumentException` on an empty or
whitespace condition rather than touching every row. To mean every row, pass
`"1 = 1"`. A null flush condition with `flushTable: true` deletes every row.

## 3. Connecting

```csharp
new SQLServerConnectionString(serverName, databaseName, bool encrypt = true, string additionalParameters = "")
new SQLServerConnectionString(serverName, databaseName, string? userId, string? password, bool encrypt = true, string additionalParameters = "")
```

`GetConnectionString()` builds the string with `SqlConnectionStringBuilder`.
A null or empty `UserId` gives `Integrated Security=True`; otherwise `User ID`
and `Password`. `additionalParameters` is parsed as a connection string and
each of its keywords replaces the one the other arguments set; text that is
not a valid connection string throws `ArgumentException`. Properties
`Server`, `DatabaseName`, `UserId`, `Password`, `Encrypt`,
`AdditionalParameters` are settable.

`new SQLServerInstance(string)` and `new SQLServerInstance(SQLServerConnectionString)`
do not connect.

## 4. Queries

Every method is async and takes an optional `CancellationToken` as its last argument.

| Method | Returns |
|---|---|
| `ExecuteQueryAsync(sql)` | `DataTable` of the whole result set |
| `ExecuteQueryAsync<T>(sql)` | `List<T>`: the first column of every row, through `Convert.ChangeType`; a nullable `T` converts to its underlying type and NULL gives null |
| `ExecuteScalarAsync<T>(sql)` | `T?`: the first column of the first row, converted the same way; `default(T)` for no rows, NULL or `DBNull` |
| `ExecuteQueryToObjectListAsync<T>(sql, parameters = null)` | `List<T>` where `T : new()`: one object per row, every public property set from the column of its name or its `[SQLServerInstance.Column("name")]` attribute, converted to the property's type (nullable aware); NULL leaves the property as constructed; a missing column throws |
| `ExecuteParameterizedQueryAsync(sql, SqlParameter[])` | nothing; runs a non-query with the given parameters. A `SqlParameter` belongs to one command, so build new ones per call |

## 5. Commands


| Method | Effect |
|---|---|
| `ExecuteSQLAsync(sql)` | runs the batch |
| `ExecuteNonQueryWithParametersAsync(sql, Dictionary<string, object>)` | runs the batch with the dictionary as parameters |
| `ExecuteStoredProcedureAsync(name, SqlParameter[]? = null)` | runs the procedure with `CommandType.StoredProcedure`; the name is sent as a procedure name, not parsed as a batch |
| `ExecuteTransactionAsync(List<string>)` | runs each batch in order in one transaction; any exception rolls back and rethrows. For anything beyond raw batches, use `BeginTransactionAsync` |
| `ExecuteScriptFromFileAsync(path)` | runs the file's contents as one batch; there is no `GO` splitting |

None return the rows affected.

## 6. Inserting, updating, deleting


| Method | Effect |
|---|---|
| `InsertDataAsync(table, Dictionary<string, object> values)` | one row; keys are column names |
| `InsertDataAsync<T>(T data, table)` where `T : class` | one row from every public instance property with a getter, column named by `[SQLServerInstance.Column]` or the property; static properties and indexers are left out. An identity column fails unless `IDENTITY_INSERT` is on |
| `UpdateDataAsync(table, values, condition = "")` and `UpdateDataAsync(table, values, condition, Dictionary<string, object>? parameters)` | `UPDATE ... SET ... WHERE condition` |
| `DeleteDataAsync(table, condition = "")` and `DeleteDataAsync(table, condition, parameters)` | `DELETE ... WHERE condition` |

An empty values dictionary, or a `T` with no usable properties, throws
`ArgumentException`. None return the rows affected.

## 7. Merging

```csharp
int MergeData(sourceTableName, targetTableName, IEnumerable<string> keyColumns, IEnumerable<string> valueColumns, bool deleteUnmatched = false, bool useTransaction = true)
int BulkMerge(DataTable dataTable, destinationTableName, IEnumerable<string> keyColumns, bool deleteUnmatched = false, int timeout = 30, int? batchSize = null, bool useTransaction = true)
```

Both return the rows inserted, updated and
deleted.

`MergeDataAsync` runs one `MERGE INTO target WITH (HOLDLOCK) USING source ON keys`:
a target row whose key columns all equal a source row's is updated in the
value columns; a source row with no match is inserted with keys and values;
with `deleteUnmatched`, a target row with no match is deleted. Empty value
columns insert the missing keys and leave matched rows alone. A column named
in both lists, or no key columns, throws `ArgumentException`. The source may
be a view.

`BulkMergeAsync` does the same for a `DataTable`, on one connection: it creates a
local temporary table shaped like the destination's columns
(`DROP TABLE IF EXISTS` first, then `SELECT TOP (0) ... INTO`), bulk copies the
rows into it, and merges from there. Columns match by name, so the DataTable's
column order does not matter; every DataTable column must exist in the
destination; every non-key column is a value column; a destination column the
DataTable lacks is left alone on update and takes its default on insert. The
`timeout` applies to the copy and then to the merge.

Rules for both:

- A NULL key never matches, so such a row is inserted on every merge.
- Two source rows with the same key fail the merge (SQL Server refuses to
  update one row twice).
- An identity column cannot be among the merged columns: SQL Server refuses
  to compile the MERGE's insert into it even when every row matches. Key an
  identity table on a natural column and leave the identity out.
- `HOLDLOCK` closes the upsert race between sessions. Concurrent merges into
  one table can still deadlock; the library does not retry. An application
  lock is the caller's choice.
- `useTransaction: false` releases the merge's locks as soon as the statement
  finishes; the statement itself is atomic either way.

## 8. Bulk copy

```csharp
void BulkCopy(DataTable dataTable, destinationTableName, bool flushTable = false, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, string? flushWhereClauseCondition = null)
void BulkCopy(DataTable dataTable, destinationTableName, bool flushTable, string? flushWhereClauseCondition, Dictionary<string, object>? flushParameters, int bulkCopyTimeout = 30, int? batchSize = null, bool useTransaction = true, IReadOnlyDictionary<string, string>? columnMappings = null)
```

`SqlBulkCopy` writes the rows. Without `columnMappings`
columns map by position; with them, each key is a DataTable column (matched
without regard to case) and its value the destination column (matched
exactly, brackets removed), and only mapped columns are copied. With
`flushTable`, rows are deleted first: all of them, or those matching the
condition. With `useTransaction` (the default) the delete and the copy are
one transaction. Argument errors are thrown before anything runs; a
destination column the table lacks fails when the copy runs, after the flush,
which the transaction rolls back.

## 9. Transactions across calls

```csharp
Task<SQLServerTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
Task<SQLServerTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
```

`IsolationLevel.Unspecified` (the default) is SqlClient's read committed.

`SQLServerTransaction` (`sealed`, `IDisposable`, `IAsyncDisposable`) carries
these with the same arguments as on `SQLServerInstance`, minus any
`useTransaction` parameter:

`ExecuteQueryAsync`, `ExecuteQueryAsync<T>`, `ExecuteScalarAsync<T>`,
`ExecuteQueryToObjectListAsync<T>`, `ExecuteParameterizedQueryAsync`, `ExecuteSQLAsync`,
`ExecuteNonQueryWithParametersAsync`, `ExecuteStoredProcedureAsync`, `InsertDataAsync`,
`InsertDataAsync<T>`, `UpdateDataAsync`, `DeleteDataAsync`, `MergeDataAsync`, `BulkCopyAsync`,
`BulkMergeAsync`, `IndexCreateAsync`, `IndexDropAsync`.

Lifecycle:

| Member | Effect |
|---|---|
| `CommitAsync(token)` | commits; no method runs afterwards |
| `RollbackAsync(token)` | rolls back early; no method runs afterwards |
| `SaveAsync(name, token)` | marks a savepoint (one identifier, at most 32 characters; the same name again moves it) |
| `RollbackToAsync(name, token)` | undoes the work since the savepoint; the transaction stays open |
| `Dispose()` / `DisposeAsync()` | rolls back if not committed, closes the connection; safe to call twice |

Rules:

- A query on it sees the transaction's own uncommitted work.
- After an exception from any method, roll back or let the `using` block do
  it. Do not catch and carry on to `CommitAsync`: SQL Server fails only the
  statement for some errors (a constraint violation, for one) and leaves the
  transaction open, so the commit would keep the earlier work.
- `RollbackToAsync` works after a statement-level error. After an error that made
  the transaction uncommittable (a deadlock, a conversion failure) SQL Server
  refuses it; only a full rollback is left.
- If `CommitAsync` throws (the connection dropped, say), every later call throws
  `InvalidOperationException` saying the commit failed; dispose the object.
- After commit, rollback or dispose, every method throws
  `InvalidOperationException` or `ObjectDisposedException`.
- Not thread-safe. One thread or one async flow at a time. Dispose promptly:
  it holds a pooled connection and the transaction's locks.
- Not on it: backup, restore, CSV export, the schema lookups,
  `ExecuteScriptFromFileAsync`, `ExecuteTransactionAsync`.

## 10. Export, backup, restore

| Method | Effect |
|---|---|
| `ExportDataToCSVAsync(destinationFilePath, sql)` | runs the query and writes the file, UTF-8 without BOM; header row unquoted, every data value in double quotes with embedded quotes doubled, NULL as `""`, values formatted with the current culture |
| `BackupDatabaseAsync(path)` | `BACKUP DATABASE` to the path, which must be reachable by the SQL Server service account; the name and path are parameters |
| `RestoreDatabaseAsync(path)` | switches the connection to `master`, then `RESTORE DATABASE ... FROM DISK` with no options; the name and path are parameters, and the database must not be in use |
| `GetLastBackupDateTimeAsync()` | the latest full backup finish time from `msdb`, or null |

## 11. Schema and server information

All read-only except the two index methods, which the transaction object also carries.

| Method | Returns |
|---|---|
| `DoesDatabaseExistAsync(name)` | whether SqlClient's Databases schema collection lists it |
| `DoesTableExistAsync(table)` | whether a table or view with that name exists; a bare name matches in any schema, `dbo.X` only in `dbo` |
| `GetTableNamesAsync()` | table and view names from SqlClient's Tables schema collection, without schema |
| `GetColumnNamesAsync(table)` | column names, from SqlClient's schema lookup |
| `GetTableColumnsAsync(table)` | `Dictionary<string, string>` of column name to data type |
| `GetTableSchemaAsync(table)` | an empty `DataTable` with the columns (`SELECT * ... WHERE 1 = 0`) |
| `GetTableRowCountAsync(table)` | `COUNT(*)` |
| `GetTablePrimaryKeyColumnAsync(table)` / `GetTablePrimaryKeyColumnsAsync(table)` | the first key column in key order, or all of them |
| `GetTableIndexesAsync(table)` | nonclustered, non-primary-key index names, from `sys.indexes` by `OBJECT_ID` (a bare name means the default schema) |
| `GetStoredProceduresAsync()` | procedure and function names, without schema |
| `GetStoredProcedureParametersAsync(name)` | parameter names with their `@`, from `DeriveParameters`; the return value is left out |
| `GetDatabaseInformationAsync()` | `DatabaseName`, `DatabaseId`, `CreationDate` (current culture), `Collation` |
| `GetDatabaseSizeInBytesAsync()` | data plus log size in bytes, from `sys.master_files` |
| `IndexCreateAsync(table, column)` | `CREATE INDEX [IX_<column>] ON <table> ([<column>])` |
| `IndexDropAsync(table, index)` | `DROP INDEX [<index>] ON <table>` |

## 12. QueryBuilder

Builds a SELECT as text; every string is placed as written. `Build()` returns
a `QueryBuildResult` with `SQL`, `Parameters` (the names, comma-joined) and
`ParameterValues` (a dictionary ready for any method that takes one). The
SELECT list is the `Select` columns followed by aggregates and CASE
expressions in call order; the other clauses follow in the order called.

Members: `Select(columns = "")`, `From(table)`, `Where`, `And`, `Or`,
`AddParameter(name, value)`, `Join(table, onCondition, JoinType = Inner)`,
`OrderBy(columns, QuerySortOrder = Ascending)`, `GroupBy`, `Count`, `Sum`,
`Avg`, `Min`, `Max` (each `(column, alias)`), `CreateSubquery()`,
`StartNestedCondition` / `EndNestedCondition`, `Paginate(page, pageSize)`,
`StartCaseStatement(column or "")`, `AddCaseWhen`, `AddCaseElse`,
`EndCaseStatement(alias)`, `Build()`.

Rules: `Paginate` needs an `OrderBy` or `Build()` throws; a page or page size
below 1 throws; ending a nested condition that holds nothing, or was never
started, throws `InvalidOperationException`; `Build()` needs a `FROM` outside
parentheses, quotes and comments; `Build()` leaves the builder unchanged.
`Union`, `Intersect` and `Except` are obsolete: they only put their keyword in
front of the builder's own SQL.

## 13. Changing the library

- Public signatures are frozen for callers within a major version: add an
  overload or a method, never a parameter on an existing public method,
  optional or not. 3.0.0 was the major version that settled this debt, by
  making every method async with a token; the next such change waits for 4.0.
- Every method is implemented once. A data method lives on
  `SQLServerTransaction`, in the file named after it, and the
  `SQLServerInstance` method in the same file is one line through `RunAsync`,
  which opens a connection, runs the call in autocommit (or in a transaction
  it commits, for the methods with `useTransaction`) and closes it. A lookup
  that needs the bare connection goes through `WithConnectionAsync` or the
  catalog helpers in `SQLServerInstance.Catalog.cs`. The XML docs are written
  on the transaction method and inherited by the instance method, except
  where the instance method has a `useTransaction` parameter to document.
- A new site that builds SQL text needs an entry in
  `.github/security/accepted.toml` with its reason, matched by file and the
  text of the flagged line, and, at an identifier site, the name of the
  integration test that runs it on a hostile name. The `SqlCommand` lines stay
  in their method files so the record stays per method.
- Tests: xUnit v3 under Microsoft.Testing.Platform. Unit tests need nothing;
  integration tests need the container that
  `scripts/test/run-integration-tests.sh` starts, and are skipped without it,
  which CI treats as failure. A fix comes with a test that fails without it.
- Release: `PackageVersion` and `AssemblyVersion` in the csproj, a
  `## [X.Y.Z] - date` section in `CHANGELOG.md`, then the owner pushes the
  `vX.Y.Z` tag. The README is packed into the package as its readme.

## 14. Breaking changes, 1.x to 3.0.0

In 3.0.0:

- Every synchronous method is gone. Each server-facing method exists only in
  its `...Async` form, and the methods that had no async form before, the
  schema lookups, `GetLastBackupDateTime`, `IndexCreate` and `IndexDrop`, are
  now `...Async` too. Upgrading is mechanical: append `Async` to the name and
  `await` the call. `QueryBuilder` and `SQLServerConnectionString` never
  touched the server and are unchanged.
- Every async method takes an optional `CancellationToken` as its last
  parameter. Existing calls compile unchanged, but the parameter changes the
  methods' IL signatures, so an assembly built against 1.x or 2.x must be
  recompiled; swapping the DLL under built code fails with
  `MissingMethodException`. A method-group conversion such as
  `Func<string, Task<DataTable>> f = db.ExecuteQueryAsync;` needs a lambda.
- `GetTableIndexs` is `GetTableIndexesAsync`.
- `ExecuteTransaction`, `BeginTransaction`, `Commit`, `Rollback`, `Save` and
  `RollbackTo` exist only as `...Async`; `SQLServerTransaction` keeps a
  synchronous `Dispose` beside `DisposeAsync` so a `using` block still works.
- Added, not breaking: `MergeDataAsync`, `BulkMergeAsync`, the transaction
  object with savepoints, and the index methods on it.

Everything below arrived in 2.0.0 and still holds. Methods are named here as
they are in 3.0.0; in 2.0.0 each also had a synchronous twin.

Names and SQL:

- A table name is read as a one-, two- or three-part name and quoted. Text
  that only worked because it was pasted into the SQL, such as an alias or a
  table hint, no longer does. A malformed name throws `ArgumentException`.
  A dictionary key, `IndexCreateAsync`'s column and `IndexDropAsync`'s index are one
  name each; bracket one that contains a dot.
- An empty or whitespace condition in `DeleteDataAsync`, `UpdateDataAsync` and
  `BulkCopyAsync`'s `flushWhereClauseCondition` throws instead of affecting every
  row. `DeleteDataAsync(table)` with no condition throws too. Pass `"1 = 1"` to
  mean every row.
- `InsertDataAsync` and `UpdateDataAsync` send values as parameters named
  `@__value_0`, `@__value_1`, ..., not as parameters named after the keys,
  and an empty values dictionary throws.
- `BackupDatabaseAsync` and `RestoreDatabaseAsync` send the database name and path as
  parameters; `RestoreDatabaseAsync` switches to `master` first.
- The table lookups send the name as parameters instead of placing it in
  quotes in the SQL, so `DoesTableExistAsync`, `GetTableColumnsAsync`, `GetColumnNamesAsync`
  and `GetTablePrimaryKeyColumnAsync` read `dbo.Orders` as schema `dbo`, table
  `Orders`; they used to look for a table named `dbo.Orders` and find none.

Values and results:

- `GetDatabaseSizeInBytesAsync` returns bytes; it returned kilobytes, so the
  result is 1024 times larger.
- `ExecuteScalarAsync<T>` and `ExecuteQueryAsync<T>` convert a
  nullable `T` instead of throwing, and give null for NULL.
- A null value in a parameter or values dictionary is sent as NULL instead
  of being dropped.
- Every method that takes a parameter dictionary adds the `@` only when a
  name lacks it; `ExecuteNonQueryWithParametersAsync` used to turn `@Name` into
  `@@Name`.
- `GetTablePrimaryKeyColumnAsync` returns the first key column in key order
  instead of an arbitrary one.

Types and mapping:

- `InsertDataAsync<T>` writes a property to the column its
  `SQLServerInstance.Column` attribute names, and leaves out static
  properties and indexers. It used the property name and took every public
  property.
- The `SortOrder` enum that `QueryBuilder.OrderBy` takes is renamed
  `QuerySortOrder`.

Connection strings:

- `SQLServerConnectionString.GetConnectionString()` builds the string with
  `SqlConnectionStringBuilder`, so its text uses SqlClient's keywords and
  quotes values that need it. Additional parameters that are not a valid
  connection string throw. The stray `;;` is gone.

QueryBuilder:

- `Paginate` without an `OrderBy` throws at `Build()`; a page or page size
  below 1 throws; ending an empty nested condition throws; `Build()` needs a
  `FROM`. `OrderBy` writes `ASC` and `DESC`. Aggregates and CASE expressions
  are joined with commas, `StartCaseStatement("")` builds a searched CASE,
  nested conditions are wrapped in parentheses, and `Build()` can be called
  twice. `Union`, `Intersect` and `Except` are obsolete.

Dependencies:

- Microsoft.Data.SqlClient 7.1.0, up from 5.2. Target framework `net9.0`.
