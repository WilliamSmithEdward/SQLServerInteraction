# SQLServerInteraction

SQLServerInteraction is a thin layer over Microsoft.Data.SqlClient for .NET 9. A `SQLServerInstance` holds a connection string, and each of its methods opens a connection, runs one piece of work (a query, a command, a bulk copy, a backup, a schema lookup) and closes the connection again. A separate `QueryBuilder` assembles SELECT statements from strings.

```
dotnet add package SQLServerInteraction
```

Everything is in the `SQLServerInteraction` namespace.

---

## Read this first: what is and is not parameterized

Values you pass in a parameter dictionary or a `SqlParameter` array are sent as SQL parameters. Everything else is pasted into the SQL text as written: table names, column names (the keys of the insert and update dictionaries), conditions, index names, file paths, and the `sql` strings you pass to the query methods. The library does not quote, escape or validate any of them.

Only pass those strings from your own code. If any part of one comes from a user, a file or another system, check it against a list of names you expect before calling the method. The section "Methods that build SQL from your strings", further down, lists every method that builds SQL this way.

---

## Connecting

Pass a connection string, or build one with `SQLServerConnectionString`:

```csharp
using SQLServerInteraction;

// Windows authentication (Trusted_Connection=True)
var trusted = new SQLServerConnectionString("your-server", "YourDatabase");
Console.WriteLine(trusted.GetConnectionString());
// Server=your-server;Database=YourDatabase;Trusted_Connection=True;Encrypt=True;;

// SQL Server authentication, encryption off, one extra keyword
var login = new SQLServerConnectionString("your-server", "YourDatabase", "your-user", "your-password",
    encrypt: false, additionalParameters: "TrustServerCertificate=True;");
Console.WriteLine(login.GetConnectionString());
// Server=your-server;Database=YourDatabase;User Id=your-user;Password=your-password;Encrypt=False;;TrustServerCertificate=True;

var db = new SQLServerInstance(trusted);
var db2 = new SQLServerInstance("Server=your-server;Database=YourDatabase;Trusted_Connection=True;Encrypt=True;");
```

How `GetConnectionString()` builds the string:

- `Encrypt` defaults to `true`.
- A null or empty `UserId` gives `Trusted_Connection=True`, and the password is ignored. Otherwise the string carries `User Id` and `Password`.
- `additionalParameters` is appended at the end as written, so end each keyword with `;`.
- Nothing is validated or escaped. A value containing `;`, such as a password, breaks the string: SqlClient then throws `ArgumentException` when the connection string is parsed. Use `SqlConnectionStringBuilder` and the string constructor of `SQLServerInstance` for such values.
- The empty `;;` after `Encrypt` is harmless; SqlClient skips it.

Constructing a `SQLServerInstance` does not connect. Each method call opens its own `SqlConnection` and disposes it before returning, so connection pooling is whatever the connection string sets (SqlClient pools by default). Commands use SqlClient's default 30-second command timeout; apart from `BulkCopy`, no method takes a timeout or a `CancellationToken`.

Errors are not caught: a server error surfaces as SqlClient's `SqlException`, and a failed connection as whatever `SqlConnection.Open` throws.

Keep server names, user names and passwords out of source code. Read them from configuration or a secret store.

---

## Queries

```csharp
using System.Data;
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

// Every row and column, as a DataTable
DataTable orders = db.ExecuteQuery("SELECT OrderId, Total FROM dbo.Orders");
DataTable ordersAsync = await db.ExecuteQueryAsync("SELECT OrderId, Total FROM dbo.Orders");

// The first column of every row, converted to T
List<int> ids = db.ExecuteQuery<int>("SELECT OrderId FROM dbo.Orders");
List<string> names = await db.ExecuteQueryAsync<string>("SELECT Name FROM dbo.Customers");

// The first column of the first row, converted to T
int count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Orders");
decimal total = await db.ExecuteScalarAsync<decimal>("SELECT SUM(Total) FROM dbo.Orders");
```

- `ExecuteQuery<T>` and `ExecuteQueryAsync<T>` read only the first column and convert each value with `Convert.ChangeType`. A NULL becomes `""` for `string` and throws `InvalidCastException` for a value type.
- `ExecuteScalar<T>` and `ExecuteScalarAsync<T>` return `default(T)` when the query returns no rows or a NULL, and otherwise convert the value with `Convert.ChangeType`. That conversion cannot target a nullable type, so `ExecuteScalar<int?>` throws `InvalidCastException` whenever the value is not NULL. Ask for `int` and expect `0` for no rows or NULL.
- None of these four take parameters. To filter on a value, use `ExecuteQueryToObjectList<T>` below, which does.

### Mapping rows to objects

`ExecuteQueryToObjectList<T>` and `ExecuteQueryToObjectListAsync<T>` create one `T` per row and set each public property from the column of the same name. `SQLServerInstance.Column` maps a property to a differently named column:

```csharp
using SQLServerInteraction;

public class Customer
{
    public int CustomerId { get; set; }

    [SQLServerInstance.Column("Customer Name")]
    public string? Name { get; set; }

    public DateTime? LastOrder { get; set; }
}
```

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

List<Customer> customers = db.ExecuteQueryToObjectList<Customer>(
    "SELECT CustomerId, [Customer Name], LastOrder FROM dbo.Customers WHERE Region = @Region",
    new Dictionary<string, object> { ["Region"] = "West" });

List<Customer> all = await db.ExecuteQueryToObjectListAsync<Customer>(
    "SELECT CustomerId, [Customer Name], LastOrder FROM dbo.Customers");
```

- `T` needs a public parameterless constructor.
- Every public property needs a column in the result. A property with no matching column throws `IndexOutOfRangeException`, and a read-only property throws `ArgumentException` once its column has a value; leave such properties out of the class or alias a column to them in the SQL.
- Column names match without regard to case. A NULL leaves the property at its default value. Other values are converted with `Convert.ChangeType` to the property type (or the underlying type of a nullable property), so a property whose type the column value cannot convert to, such as an enum, throws `InvalidCastException`.
- Parameter names work with or without the leading `@`. A null value is sent as SQL NULL.

---

## Commands that return nothing

```csharp
using Microsoft.Data.SqlClient;
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

db.ExecuteSQL("UPDATE dbo.Orders SET Archived = 1 WHERE OrderDate < '2020-01-01'");
await db.ExecuteSQLAsync("EXEC dbo.RefreshTotals");

// Dictionary keys are parameter names WITHOUT the @
db.ExecuteNonQueryWithParameters(
    "UPDATE dbo.Orders SET Status = @Status WHERE OrderId = @OrderId",
    new Dictionary<string, object> { ["Status"] = "Shipped", ["OrderId"] = 42 });

// SqlParameter objects, for control over type and size
db.ExecuteParameterizedQuery(
    "DELETE FROM dbo.Orders WHERE OrderId = @OrderId",
    [new SqlParameter("@OrderId", 42)]);

// A stored procedure by name, with optional parameters
await db.ExecuteStoredProcedureAsync("dbo.CloseOrder", [new SqlParameter("@OrderId", 42)]);
db.ExecuteStoredProcedure("dbo.RebuildIndexes");
```

- None of these return the number of rows affected or any result set; use `ExecuteQuery` or `ExecuteScalar<T>` to read data back. `ExecuteParameterizedQuery` runs a command, not a query, despite its name.
- `ExecuteNonQueryWithParameters` adds `@` to each key, so a key that already starts with `@` becomes `@@name` and the command fails.
- A null dictionary value is not sent as NULL: SqlClient omits the parameter and the server reports it missing. Pass `DBNull.Value` instead.
- The `SqlParameter` methods use the array you pass; a `SqlParameter` can belong to only one command, so build new ones for each call.

### Transactions

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

db.ExecuteTransaction([
    "UPDATE dbo.Accounts SET Balance = Balance - 100 WHERE AccountId = 1",
    "UPDATE dbo.Accounts SET Balance = Balance + 100 WHERE AccountId = 2",
]);
```

`ExecuteTransaction` and `ExecuteTransactionAsync` run the commands in order on one connection inside one transaction. If any command throws, the transaction is rolled back and the exception is rethrown; otherwise it is committed. The commands take no parameters.

### Running a script file

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

await db.ExecuteScriptFromFileAsync(@"C:\scripts\create-tables.sql");
```

The whole file is read and sent as a single batch. `GO` is a separator understood by SQL Server tools, not by the server, so a script that uses it fails; split such a script and run each part with `ExecuteSQL`. There is no synchronous version.

---

## Inserting, updating and deleting

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

// Column name -> value. Bracket names that need it.
db.InsertData("dbo.Customers", new Dictionary<string, object>
{
    ["CustomerId"] = 7,
    ["[Customer Name]"] = "Contoso",
    ["LastOrder"] = DBNull.Value,
});

db.UpdateData("dbo.Customers",
    new Dictionary<string, object> { ["[Customer Name]"] = "Contoso Ltd" },
    "CustomerId = 7");

db.DeleteData("dbo.Customers", "CustomerId = 7");

// One object -> one row
await db.InsertDataAsync(new Customer { CustomerId = 8, Name = "Fabrikam" }, "dbo.Customers");
```

- The values are sent as parameters. The table name, the column names and the condition are pasted into the SQL as written.
- Each dictionary key is used twice: as the column name in the SQL, and, with spaces turned into `_` and brackets removed, as the parameter name. A column name that needs brackets must have them in the key, and a key that is not a valid parameter name once spaces and brackets are handled (one with a `-` or `.` in it, for example) fails.
- A null value is not sent as NULL (see above); use `DBNull.Value`.
- `UpdateData` and `DeleteData` take the condition as SQL text without the `WHERE` keyword. **An empty condition means every row**: `DeleteData("dbo.Customers")` deletes the whole table.
- `InsertData<T>` and `InsertDataAsync<T>` insert one row with a column for every public property of `T`, named exactly as the property. They ignore `SQLServerInstance.Column`, so the example above writes to a column called `Name`. A null property value is sent as NULL. Include only properties that have columns; an identity column fails unless `IDENTITY_INSERT` is on.
- None of these return the number of rows affected.

### Bulk copy

```csharp
using System.Data;
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

var table = new DataTable();
table.Columns.Add("OrderId", typeof(int));
table.Columns.Add("Total", typeof(decimal));
table.Rows.Add(1, 19.99m);
table.Rows.Add(2, 5.00m);

// Append the rows
db.BulkCopy(table, "dbo.Orders");

// Delete the rows matching the condition first, then copy, in one transaction
await db.BulkCopyAsync(table, "dbo.Orders",
    flushTable: true,
    flushWhereClauseCondition: "OrderDate >= '2026-01-01'",
    bulkCopyTimeout: 120,
    batchSize: 5000);
```

`BulkCopy` and `BulkCopyAsync` write the rows with `SqlBulkCopy`. Columns map by position, not by name, so the DataTable's columns must be in the destination table's order.

| Parameter | Default | Effect |
|---|---|---|
| `flushTable` | `false` | Delete rows from the destination before copying. |
| `flushWhereClauseCondition` | `null` | With `flushTable`, delete only the rows matching this condition (SQL text without `WHERE`). Without it, every row is deleted. |
| `bulkCopyTimeout` | `30` | Seconds the copy may take before it fails. |
| `batchSize` | `null` | Rows per batch sent to the server. `null` sends all rows in one batch. |
| `useTransaction` | `true` | Run the delete and the copy in one transaction, rolled back if either fails, so readers never see the table half-written. With `false`, a failure can leave the table emptied or partly filled. |

---

## Exporting to CSV

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

await db.ExportDataToCSVAsync(@"C:\exports\orders.csv", "SELECT OrderId, Total FROM dbo.Orders");
```

The file is created or overwritten, in UTF-8 without a byte order mark. The first line holds the column names, joined with commas and not quoted. Each data row has every value in double quotes, with embedded quotes doubled; a NULL is written as `""`. Values are formatted with the current culture, so dates and decimals follow the machine's regional settings. There is no synchronous version.

---

## Backup and restore

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

db.BackupDatabase(@"D:\Backups\YourDatabase.bak");
DateTime? last = db.GetLastBackupDateTime();
await db.RestoreDatabaseAsync(@"D:\Backups\YourDatabase.bak");
```

- Both act on the database named in the connection string. The path is a path on the SQL Server machine, not on the machine running your code, and the server's service account needs access to it.
- `BackupDatabase` runs `BACKUP DATABASE [db] TO DISK = 'path'` with no options, so a second backup to the same file is appended to it.
- `RestoreDatabase` runs `USE master; RESTORE DATABASE [db] FROM DISK = 'path'` with no options (no `REPLACE`, no `MOVE`). It fails if any other connection is using the database, and idle pooled connections from this process count.
- `GetLastBackupDateTime` returns the latest `backup_finish_date` in `msdb.dbo.backupset` for the current database, or null if there is none. It needs read access to msdb.

---

## Schema and server information

| Method | Returns |
|---|---|
| `DoesDatabaseExist(databaseName)` | `true` if a database of that name exists on the server, ignoring case. |
| `DoesTableExist(tableName)` | `true` if any schema has a table or view with that name (`INFORMATION_SCHEMA.TABLES`). |
| `GetTableNames()` | The names of all tables and views in the database, without schema names. |
| `GetColumnNames(tableName)` | The column names of every table or view with that name, in any schema. |
| `GetTableColumns(tableName)` | Column name to data type name (`int`, `nvarchar`, ...), from `INFORMATION_SCHEMA.COLUMNS`. |
| `GetTableSchema(tableName)` | An empty `DataTable` with the table's columns, types and key, from `SqlDataAdapter.FillSchema`. |
| `GetTablePrimaryKeyColumn(tableName)` | The name of one primary key column, or null. For a composite key, only one of its columns. |
| `GetTableIndexs(tableName)` | The names of the table's non-clustered indexes that are not the primary key. Clustered indexes are not listed. |
| `GetTableRowCount(tableName)` | `SELECT COUNT(*)` for the table, as an `int`. |
| `GetStoredProcedures()` | The names of all stored procedures and functions (`INFORMATION_SCHEMA.ROUTINES`), without schema names. |
| `GetStoredProcedureParameters(storedProcedureName)` | The procedure's parameter names, with their `@`, from `SqlCommandBuilder.DeriveParameters`. The return value is left out. |
| `GetDatabaseInformation()` | A dictionary with `DatabaseName`, `DatabaseId`, `CreationDate` (formatted with the current culture) and `Collation`. |
| `GetDatabaseSizeInBytes()` | The total size of the database's data and log files **in kilobytes**, despite the name (`SUM(size) * 8` from `sys.master_files`, where `size` counts 8 KB pages). Needs permission to read `sys.master_files`. |
| `IndexCreate(tableName, columnName)` | Nothing. Runs `CREATE INDEX IX_<column> ON <table> (<column>)`. |
| `IndexDrop(tableName, indexName)` | Nothing. Runs `DROP INDEX <index> ON <table>`. |

Table names may include the schema (`dbo.Orders`) for `GetTableSchema`, `GetTableRowCount`, `GetTableIndexs`, `IndexCreate` and `IndexDrop`, which paste the name into SQL. `DoesTableExist`, `GetColumnNames`, `GetTableColumns` and `GetTablePrimaryKeyColumn` compare it with the bare table name, so pass `Orders`, not `dbo.Orders`.

---

## Methods that build SQL from your strings

These methods put caller strings into SQL text without parameters. Treat every string listed here as code.

| Method (and its async version) | Strings pasted into SQL |
|---|---|
| `ExecuteQuery`, `ExecuteQuery<T>`, `ExecuteScalar<T>`, `ExecuteSQL`, `ExecuteTransaction`, `ExportDataToCSVAsync`, `ExecuteScriptFromFileAsync` (file contents) | the SQL itself, by design |
| `InsertData` | table name, dictionary keys |
| `InsertData<T>` | table name (column names come from the type) |
| `UpdateData` | table name, dictionary keys, condition |
| `DeleteData` | table name, condition |
| `BulkCopy` | table name and `flushWhereClauseCondition` in the `DELETE`; the table name also goes to `SqlBulkCopy` |
| `BackupDatabase`, `RestoreDatabase` | backup file path, inside `'...'` |
| `DoesTableExist`, `GetTableColumns`, `GetTablePrimaryKeyColumn`, `GetTableIndexs` | table name, inside `'...'` |
| `GetTableRowCount`, `GetTableSchema` | table name |
| `IndexCreate` | table name, column name |
| `IndexDrop` | table name, index name |
| `QueryBuilder` (every method) | every string |

`ExecuteNonQueryWithParameters`, `ExecuteParameterizedQuery`, `ExecuteStoredProcedure` and `ExecuteQueryToObjectList<T>` send values as parameters but still run the SQL text or procedure name you give them. `DoesDatabaseExist`, `GetColumnNames`, `GetTableNames`, `GetStoredProcedures` and `GetStoredProcedureParameters` pass names to SqlClient as parameters.

---

## QueryBuilder

`QueryBuilder` appends clauses to a string in the order you call its methods, and `Build()` returns the text. It does not check the SQL, quote anything or talk to a server. Each call appends its keyword and your text followed by a space, so the order of calls is the order of the clauses.

```csharp
using SQLServerInteraction;

var queryBuilder = new QueryBuilder();
queryBuilder.Select("CustomerId, OrderDate, TotalAmount");
queryBuilder.From("Orders");
queryBuilder.Where("Status = 'Shipped'");
queryBuilder.And("TotalAmount > @MinAmount");
queryBuilder.Or("TotalAmount > (SELECT AVG(TotalAmount) FROM Orders)");
queryBuilder.AddParameter("MinAmount", 100);

QueryBuildResult result = queryBuilder.Build();
Console.WriteLine(result.SQL);
// SELECT CustomerId, OrderDate, TotalAmount FROM Orders WHERE Status = 'Shipped' AND TotalAmount > @MinAmount OR TotalAmount > (SELECT AVG(TotalAmount) FROM Orders)
Console.WriteLine(result.Parameters);
// @MinAmount
```

```csharp
using SQLServerInteraction;

var queryBuilder = new QueryBuilder();
queryBuilder.Select("Orders.OrderId, Customers.CustomerName");
queryBuilder.From("Orders");
queryBuilder.Join("Customers", "Orders.CustomerId = Customers.CustomerId", JoinType.Left);

Console.WriteLine(queryBuilder.Build().SQL);
// SELECT Orders.OrderId, Customers.CustomerName FROM Orders LEFT JOIN Customers ON Orders.CustomerId = Customers.CustomerId
```

```csharp
using SQLServerInteraction;

var queryBuilder = new QueryBuilder();
queryBuilder.Select("Category, COUNT(*) AS TotalProducts");
queryBuilder.From("Products");
queryBuilder.GroupBy("Category");

Console.WriteLine(queryBuilder.Build().SQL);
// SELECT Category, COUNT(*) AS TotalProducts FROM Products GROUP BY Category
```

- `Select()` with no argument selects `*`.
- `Join` takes `JoinType.Inner` (the default), `Left`, `Right` or `Full`.
- `Build()` throws `InvalidOperationException` unless the text contains `SELECT` and `FROM` somewhere (a plain substring test).
- `Build()` appends to the builder's text as it runs, so call it once and keep the result.
- `AddParameter` records only the name. `QueryBuildResult.Parameters` is a string such as `"@MinAmount, @MaxAmount"`; the values are not returned. Bind them yourself when you run the query:

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

var queryBuilder = new QueryBuilder();
queryBuilder.Select("CustomerId, [Customer Name], LastOrder");
queryBuilder.From("dbo.Customers");
queryBuilder.Where("Region = @Region");

List<Customer> west = db.ExecuteQueryToObjectList<Customer>(
    queryBuilder.Build().SQL!,
    new Dictionary<string, object> { ["Region"] = "West" });
```

### Known problems

These methods produce SQL that SQL Server rejects or that does not mean what the method name says. Write those parts of the query by hand instead, for example inside the `Select`, `From` or `Where` text.

- `OrderBy` appends `ASCENDING` or `DESCENDING` after the columns (`ORDER BY Price ASCENDING`); T-SQL accepts only `ASC` and `DESC`. Because `Paginate` emits `OFFSET ... FETCH`, which needs `ORDER BY`, it cannot be used either.
- `Count`, `Sum`, `Avg`, `Min` and `Max` append `COUNT(col) AS alias` with no comma before it and no space after it, so the result runs into the next clause (`SELECT * COUNT(Revenue) AS CountOfRevenueFROM Sales`).
- `StartNestedCondition` and `EndNestedCondition` do not wrap the conditions between them; `EndNestedCondition` appends `()`.
- `StartCaseStatement(alias)` starts `CASE alias WHEN ...`, a simple CASE comparing the alias with each condition, and the whole CASE is appended where `EndCaseStatement` is called, usually after `FROM`.
- `CreateSubquery` appends the subquery's type name, `(SQLServerInteraction.QueryBuildResult)`, instead of its SQL.
- `Union`, `Intersect` and `Except` put the keyword at the start of the query with nothing before it.

The `SortOrder` enum that `OrderBy` takes has the same name as `Microsoft.Data.SqlClient.SortOrder`. A file that imports both namespaces must write `SQLServerInteraction.SortOrder`.

---

## License

MIT. See [LICENSE](https://github.com/WilliamSmithEdward/SQLServerInteraction/blob/main/LICENSE).

The icon, SQLServerInteraction.png, was designed by Iconjam on Freepik.com.
