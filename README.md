# SQLServerInteraction

## Summary

This project wraps the Microsoft.Data.SqlClient library to enhance interaction with a SQL Server database. These functions cover essential tasks like optimizing data transfer from a DataTable to a designated database table using bulk copying, executing SQL queries to consolidate data into a DataTable, extracting values from SQL queries, and executing non-query SQL statements.

## Methods

**BulkCopy**

-   **Method Signature:**
    -   `public void BulkCopy(DataTable dt, string sqlServerTableName, bool flushTableBeforeCopy)`
-   **Description:** This method copies data from a DataTable to a specified SQL Server table using bulk copy.
-   **Parameters:**
    -   `dt` (DataTable): The DataTable containing the data to be copied.
    -   `sqlServerTableName` (string): The destination SQL Server table name.
    -   `flushTableBeforeCopy` (bool): Whether to flush the destination table before copying data.

**QueryIntoDataTable**

-   **Method Signature:**
    -   `public DataTable QueryIntoDataTable(string sql)`
-   **Description:** This method executes an SQL query and returns the results as a DataTable.
-   **Parameters:**
    -   `sql` (string): The SQL query to execute.
-   **Returns:**
    -   `DataTable`: A DataTable containing the results of the SQL query.

**QueryScalarAsString**

-   **Method Signature:**
    -   `public string? QueryScalarAsString(string sql)`
-   **Description:** This method executes an SQL query and returns the result as a string.
-   **Parameters:**
    -   `sql` (string): The SQL query to execute.
-   **Returns:**
    -   `string?`: The result of the SQL query as a string, or null if the result is null.

**ExecuteSQL**

-   **Method Signature:**
    -   `public void ExecuteSQL(string sql)`
-   **Description:** This method executes a non-query SQL statement.
-   **Parameters:**
    -   `sql` (string): The SQL statement to execute.
