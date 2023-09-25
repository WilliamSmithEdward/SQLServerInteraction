# SQLServerInteraction

## Summary

This project wraps the Microsoft.Data.SqlClient library to enhance interaction with a SQL Server database. These functions cover essential tasks like optimizing data transfer from a DataTable to a designated database table using bulk copying, executing SQL queries to consolidate data into a DataTable, extracting values from SQL queries, and executing non-query SQL statements.

## Methods

### BackupDatabase
Creates a backup of the SQL Server database.

### BackupDatabaseAsync
Async version: Creates a backup of the SQL Server database.

### BulkCopy
Copies a System.Data.DataTable object to a SQL Server table.

### BulkCopyAsync
Async version: Copies a System.Data.DataTable object to a SQL Server table.

### DeleteData
Deletes data from a SQL Server table based on specified conditions.

### DeleteDataAsync
Async version: Deletes data from a SQL Server table based on specified conditions.

### DoesDatabaseExist
Checks if a specific database exists on the SQL Server.

### DoesTableExist
Checks if a specific table exists in the SQL Server database.

### ExecuteNonQueryWithParameters
Executes a SQL command with parameters on the SQL Server.

### ExecuteNonQueryWithParametersAsync
Async version: Executes a SQL command with parameters on the SQL Server.

### ExecuteParameterizedQuery
Executes a parameterized SQL query in the SQL Server database.

### ExecuteParameterizedQueryAsync
Async version: Executes a parameterized SQL query in the SQL Server database.

### ExecuteQuery
Executes a SQL query in the SQL Server database.

### ExecuteQueryAsync
Async version: Executes a SQL query in the SQL Server database.

### ExecuteQueryT
Executes a SQL query and maps the results to a specified type.

### ExecuteQueryTAsync
Async version: Executes a SQL query and maps the results to a specified type.

### ExecuteQueryToObjectListT
Executes a SQL query and maps the results to a list of objects.

### ExecuteQueryToObjectListTAsync
Async version: Executes a SQL query and maps the results to a list of objects of a specified type.

#### Note: ExecuteQueryToObjectList logic supports mapping properties to attributes like below. If no attribute is specified, then the logic will attempt to map directly to the property name.

```csharp
public class YourClass
{
    [SQLServerInstance.Column("Your Column")]   
    public string? YourColumn { get; set; }
}
```

### ExecuteScalarT
Executes a SQL query and returns a scalar result of a specified type.

### ExecuteScalarTAsync
Async version: Executes a SQL query and returns a scalar result of a specified type.

### ExecuteScriptFromFileAsync
Async version: Executes a SQL script from a file in the SQL Server database.

### ExecuteSQL
Executes a SQL command on the SQL Server.

### ExecuteSQLAsync
Async version: Executes a SQL command on the SQL Server.

### ExecuteStoredProcedure
Executes a stored procedure in the SQL Server database.

### ExecuteStoredProcedureAsync
Async version: Executes a stored procedure in the SQL Server database.

### ExecuteTransaction
Executes a series of SQL commands as a transaction.

### ExecuteTransactionAsync
Async version: Executes a series of SQL commands as a transaction.

### ExportDataToCSVAsync
Async version: Exports data from the SQL Server to a CSV file.

### GetColumnNames
Retrieves the column names of a specific table in the SQL Server database.

### GetDatabaseInformation
Retrieves general information about the SQL Server database.

### GetDatabaseSizeInBytes
Retrieves the size of the SQL Server database in bytes.

### GetLastBackupDateTime
Retrieves the date and time of the last database backup.

### GetStoredProcedureParameters
Retrieves the parameters of a stored procedure.

### GetTableColumns
Retrieves column details (name, data type, etc.) for a specific table.

### GetTableIndexs
Retrieves index information for a specific table.

### GetTableNames
Retrieves the names of all tables in the SQL Server database.

### GetTablePrimaryKeyColumn
Retrieves the primary key column of a specific table.

### GetTableSchema
Retrieves the schema (structure) of a specific table in the SQL Server database.

### GetTableRowCount
Retrieves the row count for a specific table.

### GetStoredProcedures
Retrieves the names of stored procedures in the SQL Server database.

### IndexCreate
Creates an index on a specific table.

### IndexDrop
Drops an index on a specific table.

### InsertData
Inserts data into a SQL Server table.

### InsertDataAsync
Async version: Inserts data into a SQL Server table.

### InsertDataT
Inserts data of a specific type into a SQL Server table.

### InsertDataTAsync
Async version: Inserts data of a specific type into a SQL Server table.

### RestoreDatabase
Restores a SQL Server database from a backup.

### RestoreDatabaseAsync
Async version: Restores a SQL Server database from a backup.

### UpdateData
Updates existing data in a SQL Server table based on certain conditions.

### UpdateDataAsync
Async version: Updates existing data in a SQL Server table based on certain conditions.

## QueryBuilder

QueryBuilder is a class designed to dynamically build SQL queries in a structured and convenient manner.

### Examples

#### Basic Query
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("*");
queryBuilder.From("Users");
queryBuilder.Where("Age > @Age");
queryBuilder.AddParameter("Age", 30);

string query = queryBuilder.Build().SQL;
```

#### Combining Conditions
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("*");
queryBuilder.From("Employees");
queryBuilder.Where("Age > 30");
queryBuilder.And("Salary > 50000");
queryBuilder.Or("Department = 'HR'");

string query = queryBuilder.Build().SQL;
```

#### Table Joins
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("Orders.OrderId, Customers.CustomerName");
queryBuilder.From("Orders");
queryBuilder.Join("Customers", "Orders.CustomerId = Customers.CustomerId");

string query = queryBuilder.Build().SQL;
```

#### Subqueries
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("CustomerId, OrderDate, TotalAmount");
queryBuilder.From("Orders");
queryBuilder.Where("TotalAmount > (SELECT AVG(TotalAmount) FROM Orders)");

string query = queryBuilder.Build().SQL;
```

#### Group By
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("Category, COUNT(*) AS TotalProducts");
queryBuilder.From("Products");
queryBuilder.GroupBy("Category");

string query = queryBuilder.Build().SQL;
```

#### Aggregates (SUM, COUNT, AVG, MIN, MAX)
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select();
queryBuilder.Count("Revenue", "CountOfRevenue")
queryBuilder.From("Sales");
queryBuilder.Count("Orders");
queryBuilder.Build();
```

#### Parameters
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("*");
queryBuilder.From("Products");
queryBuilder.Where("Price > @MinPrice");
queryBuilder.AddParameter("MinPrice", 50);

string query = queryBuilder.Build().SQL;
string parameters = queryBuilder.Build().Parameters //Do something with the parameters to bind them
```

#### Pagination
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("*");
queryBuilder.From("Products");
queryBuilder.OrderBy("Price DESC");
queryBuilder.Paginate(page: 2, pageSize: 10);

string query = queryBuilder.Build().SQL;
```

#### Multiple Conditions
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("*");
queryBuilder.From("Orders");
queryBuilder.Where("Status = 'Shipped'");
queryBuilder.And("TotalAmount > @MinAmount");
queryBuilder.And("TotalAmount < @MaxAmount");
queryBuilder.AddParameter("MinAmount", 100);
queryBuilder.AddParameter("MaxAmount", 500);

string query = queryBuilder.Build().SQL;
```

#### Nested Conditions
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("*");
queryBuilder.From("Users");
queryBuilder.StartNestedCondition();
queryBuilder.And("Age > @MinAge");
queryBuilder.Or("Age < @MaxAge");
queryBuilder.EndNestedCondition();
queryBuilder.AddParameter("MinAge", 20);
queryBuilder.AddParameter("MaxAge", 50);

string query = queryBuilder.Build().SQL;
```

#### Case Statement
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("FirstName, LastName");
queryBuilder.From("Users");
queryBuilder.StartCaseStatement("AgeCategory");
queryBuilder.AddCaseWhen("Age < 18", "'Minor'");
queryBuilder.AddCaseWhen("Age >= 18 AND Age < 60", "'Adult'");
queryBuilder.AddCaseElse("'Senior'");
queryBuilder.EndCaseStatement("AgeCategory");

string query = queryBuilder.Build().SQL;
```

#### Complex Where Conditions
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("*");
queryBuilder.From("Employees");
queryBuilder.Where("Age > 25");
queryBuilder.And("Salary > 50000");
queryBuilder.And("((Department = 'IT' AND Position = 'Manager') OR (Department = 'HR'))");

string query = queryBuilder.Build().SQL;
```

#### Subquery with Join
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("Employees.Name, Departments.DepartmentName");
queryBuilder.From("Employees");
queryBuilder.Join("(SELECT DepartmentId, DepartmentName FROM Departments) AS Departments", "Employees.DepartmentId = Departments.DepartmentId");

string query = queryBuilder.Build().SQL;
```

#### Case Statement with Nested Conditions
```csharp
var queryBuilder = new QueryBuilder();
queryBuilder.Select("FirstName, LastName, Age");
queryBuilder.From("Users");
queryBuilder.StartCaseStatement("AgeCategory");
queryBuilder.AddCaseWhen("Age < 18", "'Minor'");
queryBuilder.StartNestedCondition();
queryBuilder.And("Age >= 18");
queryBuilder.Or("Age <= 60");
queryBuilder.EndNestedCondition();
queryBuilder.AddCaseElse("'Senior'");
queryBuilder.EndCaseStatement("AgeCategory");

string query = queryBuilder.Build().SQL;
```
