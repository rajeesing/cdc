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
#Change Data Capture In Action
After enabling CDC on both you should see minimum set of tables created under System Tables with cdc schema:
1. cdc.captured_columns
2. cdc.change_tables
3. cdc.dbo_Employee_CT
4. cdc.ddl_history
5. cdc.index_columns
6. cdc.lsn_time_mapping
7. dbo.systransschemas

Try to insert a row in the employee table which has following schema definitions
```
Table: Employee
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[MiddleName] [nvarchar](50) NULL,
	[LastName] [nvarchar](50) NOT NULL,
	[DesignationId] [int] NOT NULL,
```
Now insert a row to Employee table
```
INSERT INTO dbo.Employee VALUES('Sally','J.','Smith',1)
INSERT INTO dbo.Employee VALUES('John','N.','Doe',1)
```
Output:
| __$start_lsn           | __$end_lsn | __$seqval              | __$operation | __$update_mask | Id | FirstName | MiddleName | LastName | DesignationId | __$command_id |
|------------------------|------------|------------------------|--------------|----------------|----|-----------|------------|----------|---------------|---------------|
| 0x0000002A000008990004 | NULL       | 0x0000002A000008990003 | 2            | 0x1F           | 2  | Sally     | J.         | Smith    | 1             | 1             |
| 0x0000002A000008B90003 | NULL       | 0x0000002A000008B90002 | 3            | 0x0A           | 1  | John      | N.         | Doe      | 1             | 1             |

Now let's update one:
```
  Update [dbo].[Employee] SET
  FirstName = 'Johnn',
  LastName = 'Doee'
  WHERE ID = 1
```

Output:
| __$start_lsn           | __$end_lsn | __$seqval              | __$operation | __$update_mask | Id | FirstName | MiddleName | LastName | DesignationId | __$command_id |
|------------------------|------------|------------------------|--------------|----------------|----|-----------|------------|----------|---------------|---------------|
| 0x0000002A000008990004 | NULL       | 0x0000002A000008990003 | 2            | 0x1F           | 2  | Sally     | J.         | Smith    | 1             | 1             |
| 0x0000002A000008B90003 | NULL       | 0x0000002A000008B90002 | 3            | 0x0A           | 1  | John      | N.         | Doe      | 1             | 1             |
| 0x0000002A000008B90003 | NULL       | 0x0000002A000008B90002 | 4            | 0x0A           | 1  | Johnn     | N.         | Doee     | 1             | 1             |
