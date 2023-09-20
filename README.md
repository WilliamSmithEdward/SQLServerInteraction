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

### ExportDataToXLSXAsync
Async version: Exports data from the SQL Server to an XLSX file.

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
