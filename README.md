# About Change Data Capture (CDC)
Change data capture utilizes the SQL Server Agent to log insertions, updates, and deletions occurring in a table. So, it makes these data changes accessible to be easily consumed using a relational format.

# Configure for CDC
We are using SQL Server as a database for all our CDC activity with sysadmin permission. 

## Pre-requisit
Change data capture is only available in the Enterprise, Developer, Enterprise Evaluation, and Standard editions.

Create or Find database in which you want to enable CDC and use execute below script (easier when you use Microsoft SQL Server Management Studio, if you don't have install from here)

```
USE cdcexample
GO
EXEC sys.sp_cdc_enable_db
GO
```
Note: Here cdcexample is the name of database. Please replace with your database name.

If you wish to disabled the CDC, you can use below script:

```
USE cdcexample
GO
EXEC sys.sp_cdc_disable_db
GO
```
Now your database has enabled CDC, this is the time to identify which table that you wanted to capture activities (insert, update, delete DML operations) and use below script to enable CDC on table:

```
USE cdcexample
GO

EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name   = N'Employee', --table name
    @role_name     = N'MyRole', --A role for controlling access to a change table, if you don't want to grant any role, simply assign NULL
    @supports_net_changes = 1 -- if it is set to 1, a net changes function is also generated for the capture instance. This function returns only one change for each distinct row changed in the interval specified in the call.
GO
```
If you wanted to disabled the CDC on table, use below script:
```
USE cdcexample
GO
    EXEC sys.sp_cdc_disable_table
    @source_schema = N'dbo',
    @source_name   = N'Employee',
    @capture_instance = N'dbo_Employee' -- this you can find in change_tables entity by expanding your database explorer and System Tables folder in tree view.
GO
```
