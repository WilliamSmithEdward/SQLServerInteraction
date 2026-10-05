# SQLServerInteraction

[![NuGet version](https://img.shields.io/nuget/v/SQLServerInteraction)](https://www.nuget.org/packages/SQLServerInteraction)
[![Downloads](https://img.shields.io/nuget/dt/SQLServerInteraction)](https://www.nuget.org/packages/SQLServerInteraction)
[![CI](https://github.com/WilliamSmithEdward/SQLServerInteraction/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SQLServerInteraction/actions/workflows/ci.yml)
[![Security](https://github.com/WilliamSmithEdward/SQLServerInteraction/actions/workflows/security.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SQLServerInteraction/actions/workflows/security.yml)
[![Malware scan](https://github.com/WilliamSmithEdward/SQLServerInteraction/actions/workflows/malware-scan.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SQLServerInteraction/actions/workflows/malware-scan.yml)
[![OpenSSF Scorecard](https://img.shields.io/ossf-scorecard/github.com/WilliamSmithEdward/SQLServerInteraction?label=openssf%20score)](https://scorecard.dev/viewer/?uri=github.com/WilliamSmithEdward/SQLServerInteraction)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://github.com/WilliamSmithEdward/SQLServerInteraction/blob/main/LICENSE)

SQLServerInteraction is a thin layer over Microsoft.Data.SqlClient for .NET 9. A `SQLServerInstance` holds a connection string, and each of its methods opens a connection, runs one piece of work (a query, a command, a bulk copy, a backup, a schema lookup) and closes the connection again. A separate `QueryBuilder` assembles SELECT statements from strings.

```
dotnet add package SQLServerInteraction
```

Everything is in the `SQLServerInteraction` namespace.

---

## Read this first: what is and is not parameterized

Values you pass in a parameter dictionary or a `SqlParameter` array are sent as SQL parameters. Table, column, index and database names are quoted as identifiers, so a name is only ever read as a name. Backup paths are sent as parameters.

Some strings are SQL by design and are run as written: the `sql` strings you pass to the query and command methods, the conditions of `UpdateData`, `DeleteData` and `BulkCopy`, and every string `QueryBuilder` takes, table names included. Build them in your own code, and put any value that comes from a user, a file or another system in the parameters instead. The section "Methods that run your SQL", further down, lists them.

---

## Connecting

Pass a connection string, or build one with `SQLServerConnectionString`:

```csharp
using SQLServerInteraction;

// Windows authentication (Integrated Security)
var trusted = new SQLServerConnectionString("your-server", "YourDatabase");
Console.WriteLine(trusted.GetConnectionString());
// Data Source=your-server;Initial Catalog=YourDatabase;Integrated Security=True;Encrypt=True

// SQL Server authentication, encryption off, one extra keyword
var login = new SQLServerConnectionString("your-server", "YourDatabase", "your-user", "your-password",
    encrypt: false, additionalParameters: "TrustServerCertificate=True;");
Console.WriteLine(login.GetConnectionString());
// Data Source=your-server;Initial Catalog=YourDatabase;User ID=your-user;Password=your-password;Encrypt=False;Trust Server Certificate=True

var db = new SQLServerInstance(trusted);
var db2 = new SQLServerInstance("Server=your-server;Database=YourDatabase;Trusted_Connection=True;Encrypt=True;");
```

How `GetConnectionString()` builds the string:

- It uses SqlClient's `SqlConnectionStringBuilder`, so a value containing `;`, `=` or a quote, such as a password, is quoted and reaches the server as written.
- `Encrypt` defaults to `true`.
- A null or empty `UserId` gives `Integrated Security=True`, and the password is ignored. Otherwise the string carries `User ID` and `Password`.
- `additionalParameters` is parsed as a connection string and merged in. Each of its keywords replaces the one the other arguments set, as it did when 1.x appended it to the end, so `"Encrypt=Strict"` there gives strict encryption. Text that is not a valid connection string, or an unknown keyword, throws `ArgumentException`.

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

- `ExecuteQuery<T>` and `ExecuteQueryAsync<T>` read only the first column and convert each value with `Convert.ChangeType`. A nullable type such as `int?` converts to its underlying type, and NULL gives null. Otherwise a NULL becomes `""` for `string` and throws `InvalidCastException` for a value type.
- `ExecuteScalar<T>` and `ExecuteScalarAsync<T>` return `default(T)` when the query returns no rows or a NULL, and otherwise convert the value with `Convert.ChangeType`. A nullable type converts to its underlying type, so `ExecuteScalar<int?>` returns the number, or null for no rows or NULL.
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

// Dictionary keys are parameter names, with or without the @
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
- Every method that takes a parameter dictionary names the parameters the same way: the `@` is added only when the key lacks it, so `"Status"` and `"@Status"` are both `@Status`.
- A null dictionary value is sent as SQL NULL, as `DBNull.Value` is.
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

// Column name -> value
db.InsertData("dbo.Customers", new Dictionary<string, object>
{
    ["CustomerId"] = 7,
    ["Customer Name"] = "Contoso",
    ["LastOrder"] = DBNull.Value,
});

// The condition is SQL; its values go in the last argument
db.UpdateData("dbo.Customers",
    new Dictionary<string, object> { ["Customer Name"] = "Contoso Ltd" },
    "CustomerId = @Id",
    new Dictionary<string, object> { ["Id"] = 7 });

db.DeleteData("dbo.Customers", "CustomerId = @Id", new Dictionary<string, object> { ["Id"] = 7 });

// One object -> one row
await db.InsertDataAsync(new Customer { CustomerId = 8, Name = "Fabrikam" }, "dbo.Customers");
```

- Table names can have one, two or three parts (`Customers`, `dbo.Customers`, `MyDb.dbo.Customers`), each plain or in brackets (`[dbo].[My Customers]`). Every part is quoted, so a name with a space or any other character works and cannot change the SQL. A malformed name (an empty part, an unclosed bracket, more than three parts, a part over 128 characters) throws `ArgumentException`.
- Each dictionary key is one column name, plain or bracketed (`Customer Name` or `[Customer Name]`), and is quoted the same way. Bracket a column name that contains a dot. The values are sent as parameters named `@__value_0`, `@__value_1` and so on, so do not give condition parameters those names.
- A null value is sent as NULL, as `DBNull.Value` is.
- `UpdateData` and `DeleteData` take the condition as SQL text without the `WHERE` keyword, run as written. An overload of each (and of the async versions) takes a dictionary of parameters for the condition as its last argument. Parameter names work with or without the `@`, and null is sent as NULL.
- The condition is required. An empty or whitespace condition throws `ArgumentException` instead of affecting every row; to update or delete every row, pass `"1 = 1"`.
- `InsertData<T>` and `InsertDataAsync<T>` insert one row with a column for every public instance property of `T` that has a getter, named by its `SQLServerInstance.Column` attribute or else by the property, so the example above writes to `Customer Name`. Static properties and indexers are left out. A null property value is sent as NULL. Include only properties that have columns; an identity column fails unless `IDENTITY_INSERT` is on.
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
    flushWhereClauseCondition: "OrderDate >= @From",
    flushParameters: new Dictionary<string, object> { ["From"] = new DateTime(2026, 1, 1) },
    bulkCopyTimeout: 120,
    batchSize: 5000);

// Copy by name: DataTable column -> destination column. Unmapped columns are not copied.
var source = new DataTable();
source.Columns.Add("Code", typeof(int));
source.Columns.Add("Amount", typeof(decimal));
source.Rows.Add(3, 12.50m);

db.BulkCopy(source, "dbo.Orders", flushTable: false, flushWhereClauseCondition: null, flushParameters: null,
    columnMappings: new Dictionary<string, string> { ["Code"] = "OrderId", ["Amount"] = "Total" });
```

`BulkCopy` and `BulkCopyAsync` write the rows with `SqlBulkCopy`. Without `columnMappings`, columns map by position, not by name, so the DataTable's columns must be in the destination table's order. The table name is quoted as described above, for the `DELETE` and for `SqlBulkCopy`.

| Parameter | Default | Effect |
|---|---|---|
| `flushTable` | `false` | Delete rows from the destination before copying. |
| `flushWhereClauseCondition` | `null` | With `flushTable`, delete only the rows matching this condition (SQL text without `WHERE`, run as written). Left null, every row is deleted. An empty or whitespace string throws `ArgumentException`. |
| `flushParameters` | none | Parameters for `flushWhereClauseCondition`, taken by an overload whose first five arguments are `dataTable`, `destinationTableName`, `flushTable`, `flushWhereClauseCondition` and `flushParameters`. Names work with or without the `@`, and null is sent as NULL. |
| `bulkCopyTimeout` | `30` | Seconds the copy may take before it fails. |
| `batchSize` | `null` | Rows per batch sent to the server. `null` sends all rows in one batch. |
| `useTransaction` | `true` | Run the delete and the copy in one transaction, rolled back if either fails, so readers never see the table half-written. With `false`, a failure can leave the table emptied or partly filled. |
| `columnMappings` | `null` | Taken by the same overload as `flushParameters`, as its last argument. Each key is a DataTable column, matched without regard to case, and its value the destination column it is copied to; only mapped columns are copied, and the others in the destination get their defaults. A destination is a column name, not SQL, matched exactly, case included; brackets around it (`[Sales Region]`) are removed first, and a name with a space needs none. An empty dictionary, a key the DataTable does not have, or an empty destination throws `ArgumentException` before anything is deleted. A destination the table does not have fails when the copy runs, after the flush, which the transaction rolls back. `null` maps by position. |

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
- `BackupDatabase` runs `BACKUP DATABASE @database TO DISK = @path` with no options, the name and path sent as parameters, so a second backup to the same file is appended to it.
- `RestoreDatabase` switches its connection to `master`, then runs `RESTORE DATABASE @database FROM DISK = @path` with no options (no `REPLACE`, no `MOVE`). It fails if any other connection is using the database, and idle pooled connections from this process count: call `SqlConnection.ClearAllPools()` first.
- `GetLastBackupDateTime` returns the latest `backup_finish_date` in `msdb.dbo.backupset` for the current database, or null if there is none. It needs read access to msdb.

---

## Schema and server information

| Method | Returns |
|---|---|
| `DoesDatabaseExist(databaseName)` | `true` if a database of that name exists on the server, ignoring case. |
| `DoesTableExist(tableName)` | `true` if a table or view with that name exists (`INFORMATION_SCHEMA.TABLES`). |
| `GetTableNames()` | The names of all tables and views in the database, without schema names. |
| `GetColumnNames(tableName)` | The column names of every table or view with that name. |
| `GetTableColumns(tableName)` | Column name to data type name (`int`, `nvarchar`, ...), from `INFORMATION_SCHEMA.COLUMNS`, in column order. |
| `GetTableSchema(tableName)` | An empty `DataTable` with the table's columns, types and key, from `SqlDataAdapter.FillSchema`. |
| `GetTablePrimaryKeyColumn(tableName)` | The first column of the primary key in key order, or null. For a composite key, only that one. |
| `GetTablePrimaryKeyColumns(tableName)` | Every column of the primary key, in key order, or an empty list. |
| `GetTableIndexs(tableName)` | The names of the table's non-clustered indexes that are not the primary key. Clustered indexes are not listed. |
| `GetTableRowCount(tableName)` | `SELECT COUNT(*)` for the table, as an `int`. |
| `GetStoredProcedures()` | The names of all stored procedures and functions (`INFORMATION_SCHEMA.ROUTINES`), without schema names. |
| `GetStoredProcedureParameters(storedProcedureName)` | The procedure's parameter names, with their `@`, from `SqlCommandBuilder.DeriveParameters`. The return value is left out. |
| `GetDatabaseInformation()` | A dictionary with `DatabaseName`, `DatabaseId`, `CreationDate` (formatted with the current culture) and `Collation`. |
| `GetDatabaseSizeInBytes()` | The total size of the database's data and log files in bytes (`SUM(size) * 8192` from `sys.master_files`, where `size` counts 8 KB pages). Before 2.0.0 it returned kilobytes. Needs permission to read `sys.master_files`. |
| `IndexCreate(tableName, columnName)` | Nothing. Runs `CREATE INDEX [IX_<column>] ON <table> ([<column>])`. |
| `IndexDrop(tableName, indexName)` | Nothing. Runs `DROP INDEX [<index>] ON <table>`. |

Every table name takes the forms described under "Inserting, updating and deleting": one to three parts, plain or bracketed. `GetTableSchema`, `GetTableRowCount`, `IndexCreate` and `IndexDrop` quote it into the SQL, and `GetTableIndexs` passes the quoted name to `OBJECT_ID` as a parameter, so a bare name there means the default schema. `DoesTableExist`, `GetColumnNames`, `GetTableColumns`, `GetTablePrimaryKeyColumn` and `GetTablePrimaryKeyColumns` compare the parts with the catalog as parameters: a bare name (`Orders`) matches a table of that name in any schema, and `dbo.Orders` matches only the one in `dbo`.

---

## Methods that run your SQL

These methods run SQL text you write. Treat every string listed here as code: build it in your own code, and send anything else as a parameter.

| Method (and its async version) | SQL run as written |
|---|---|
| `ExecuteQuery`, `ExecuteQuery<T>`, `ExecuteScalar<T>`, `ExecuteSQL`, `ExecuteTransaction`, `ExportDataToCSVAsync`, `ExecuteScriptFromFileAsync` (file contents) | the SQL itself |
| `ExecuteNonQueryWithParameters`, `ExecuteParameterizedQuery`, `ExecuteQueryToObjectList<T>` | the SQL itself; values go as parameters |
| `UpdateData`, `DeleteData` | the condition; its values can go in the parameters dictionary their overloads take |
| `BulkCopy` | `flushWhereClauseCondition`; its values can go in `flushParameters` |
| `QueryBuilder` (every method) | every string |

`ExecuteStoredProcedure` and `GetStoredProcedureParameters` send the procedure name to SqlClient as a procedure name (`CommandType.StoredProcedure`), not as a batch. Every other name the library takes, of a table, column, index or database, is quoted or sent as a parameter.

---

## QueryBuilder

`QueryBuilder` assembles a SELECT statement from SQL fragments, and `Build()` returns the text with the parameters you recorded. Every string it takes is SQL and is placed as written, table names included: build those strings in your own code and pass values with `AddParameter`. It does not talk to a server.

The SELECT list comes first: the columns given to `Select`, then any aggregates and CASE expressions, in call order and separated by commas. The other clauses follow in the order you call their methods, each as its keyword, your text and a space.

```csharp
using SQLServerInteraction;

var db = new SQLServerInstance(new SQLServerConnectionString("your-server", "YourDatabase"));

var queryBuilder = new QueryBuilder();
queryBuilder.Select("CustomerId, [Customer Name], LastOrder");
queryBuilder.From("dbo.Customers");
queryBuilder.Where("Region = @Region");
queryBuilder.StartNestedCondition();
queryBuilder.And("LastOrder >= @Since");
queryBuilder.Or("LastOrder IS NULL");
queryBuilder.EndNestedCondition();
queryBuilder.OrderBy("CustomerId", QuerySortOrder.Descending);
queryBuilder.Paginate(page: 2, pageSize: 50);
queryBuilder.AddParameter("Region", "West");
queryBuilder.AddParameter("Since", new DateTime(2026, 1, 1));

QueryBuildResult result = queryBuilder.Build();
Console.WriteLine(result.SQL);
// SELECT CustomerId, [Customer Name], LastOrder FROM dbo.Customers WHERE Region = @Region AND (LastOrder >= @Since OR LastOrder IS NULL) ORDER BY CustomerId DESC OFFSET 50 ROWS FETCH NEXT 50 ROWS ONLY
Console.WriteLine(result.Parameters);
// @Region, @Since

// The values go with the SQL to any method that takes a parameter dictionary
List<Customer> page = db.ExecuteQueryToObjectList<Customer>(result.SQL!, result.ParameterValues);
```

```csharp
using SQLServerInteraction;

var queryBuilder = new QueryBuilder();
queryBuilder.Select("Category");
queryBuilder.Count("*", "Products");
queryBuilder.Avg("Price", "AveragePrice");
queryBuilder.StartCaseStatement("");
queryBuilder.AddCaseWhen("MAX(Price) > 100", "'premium'");
queryBuilder.AddCaseElse("'standard'");
queryBuilder.EndCaseStatement("Tier");
queryBuilder.From("Products");
queryBuilder.Join("Suppliers s", "s.SupplierId = Products.SupplierId", JoinType.Left);
queryBuilder.GroupBy("Category");

Console.WriteLine(queryBuilder.Build().SQL);
// SELECT Category, COUNT(*) AS Products, AVG(Price) AS AveragePrice, CASE WHEN MAX(Price) > 100 THEN 'premium' ELSE 'standard' END AS Tier FROM Products LEFT JOIN Suppliers s ON s.SupplierId = Products.SupplierId GROUP BY Category
```

```csharp
using SQLServerInteraction;

var queryBuilder = new QueryBuilder();
queryBuilder.Select("OrderId, Total");
queryBuilder.From("dbo.Orders");
queryBuilder.Where("CustomerId IN");
QueryBuilder customers = queryBuilder.CreateSubquery();
customers.Select("CustomerId");
customers.From("dbo.Customers");
customers.Where("Region = @Region");
customers.AddParameter("Region", "West");

Console.WriteLine(queryBuilder.Build().SQL);
// SELECT OrderId, Total FROM dbo.Orders WHERE CustomerId IN (SELECT CustomerId FROM dbo.Customers WHERE Region = @Region)
```

- `Select()` with no argument selects `*`, unless an aggregate or CASE adds to the list.
- `Count`, `Sum`, `Avg`, `Min` and `Max` add `FUNC(column) AS alias` to the SELECT list. `Count("*", alias)` counts rows.
- `StartCaseStatement("")` starts a searched CASE, whose `AddCaseWhen` conditions are predicates. `StartCaseStatement(expression)` starts a simple CASE that compares the expression with each `AddCaseWhen` value. `EndCaseStatement(alias)` adds the CASE to the SELECT list.
- `StartNestedCondition` opens a parenthesis after the keyword of the next `Where`, `And` or `Or`, and `EndNestedCondition` closes it after the last condition. Nested conditions can nest. `Build()` closes any left open; ending one that holds no condition, or that was never started, throws `InvalidOperationException`.
- `CreateSubquery` returns a builder whose SQL `Build()` places, in parentheses, where `CreateSubquery` was called. Its parameters come back with the outer query's.
- `OrderBy` writes `ASC` or `DESC` after the last column. `QuerySortOrder` was called `SortOrder` before 2.0.0, which clashed with `Microsoft.Data.SqlClient.SortOrder`.
- `Paginate(page, pageSize)` appends `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY` at the end. It needs an `OrderBy`; without one, `Build()` throws `InvalidOperationException`. A page or page size below 1 throws `ArgumentOutOfRangeException`.
- `Join` takes `JoinType.Inner` (the default), `Left`, `Right` or `Full`.
- `AddParameter` names work with or without the `@`. `QueryBuildResult.Parameters` lists the names, such as `"@Region, @Since"`, and `QueryBuildResult.ParameterValues` holds the values by name, with null as `DBNull.Value`.
- `Build()` reads the finished SQL and throws `InvalidOperationException` unless it starts with `SELECT` and has a `FROM` outside parentheses, quotes and comments. It does not change the builder, so calling it again returns the same result.

### Union, Intersect and Except

`Union()`, `Intersect()` and `Except()` are obsolete. They put their keyword in front of the builder's own SQL, so the result is only half of a query, and a single builder cannot hold the other half. Build each query and join the two:

```csharp
using SQLServerInteraction;

var current = new QueryBuilder();
current.Select("CustomerId");
current.From("dbo.Orders");

var archived = new QueryBuilder();
archived.Select("CustomerId");
archived.From("dbo.ArchivedOrders");

string sql = current.Build().SQL + "UNION " + archived.Build().SQL;
// SELECT CustomerId FROM dbo.Orders UNION SELECT CustomerId FROM dbo.ArchivedOrders
```

---

## License

MIT. See [LICENSE](https://github.com/WilliamSmithEdward/SQLServerInteraction/blob/main/LICENSE).

The icon, SQLServerInteraction.png, was designed by Iconjam on Freepik.com.
