# About Change Data Capture (CDC)
Change data capture utilizes the SQL Server Agent to log insertions, updates, and deletions occurring in a table. So, it makes these data changes accessible to be easily consumed using a relational format. [Click here](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-data-capture-sql-server?view=sql-server-2017) for more details.

# Configure for CDC
We are using SQL Server as a database for all our CDC activity with sysadmin permission. 

## Pre-requisit
Change data capture is only available in the Enterprise, Developer, Enterprise Evaluation, and Standard editions.

Create or Find the database in which you want to enable CDC and execute below script (easier when you use Microsoft SQL Server Management Studio. If you don't have one, install it from here https://aka.ms/ssmsfullsetup). Just make sure SQL Server Agent is enabled which you can do from Windows Services (type services.msc in run window [⊞ + R])

```
USE cdcexample
GO
EXEC sys.sp_cdc_enable_db
GO
```
Note: Here cdcexample is the name of the database. Please replace it with your database name.

If you wish to disable the CDC, you can use the below script:

```
USE cdcexample
GO
EXEC sys.sp_cdc_disable_db
GO
```
Now your database has enabled CDC, this is the time to identify which table that you want to capture activities (insert, update, delete DML operations) and use the below script to enable CDC on the table:

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
If you wanted to disable the CDC on the table, use the below script:
```
USE cdcexample
GO
    EXEC sys.sp_cdc_disable_table
    @source_schema = N'dbo',
    @source_name   = N'Employee',
    @capture_instance = N'dbo_Employee' -- this you can find in the change_tables entity by expanding your database explorer and System Tables folder in the tree view.
GO
```
# Change Data Capture Setup
After enabling CDC on both you should see a minimum set of tables created under System Tables with cdc schema:
1. cdc.captured_columns
2. cdc.change_tables
3. cdc.dbo_Employee_CT
4. cdc.ddl_history
5. cdc.index_columns
6. cdc.lsn_time_mapping
7. dbo.systransschemas

Try to insert a row in the employee table which has the following schema definitions
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
INSERT INTO dbo.Employee VALUES('John','N.','Doe',1);
INSERT INTO dbo.Employee VALUES('Sally','J.','Smith',1);
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

# Data Utilization
## Problem
Let's look what we have been doing. Whenever we need to gather change information, we are transfer batch of data from source to destination at regular intervals or taking a backups of tables where data are residing and restore to the location where we want. The value we are missing here is real time analytics and this might be the issue in terms for resource utilization and data inconsistencies in some cases.

![image](https://github.com/rajeesing/cdc/assets/7796293/bf01ab25-37a1-437b-b133-58659b70a847)


# Solution
**Debezium** is open source distributed platform to capture changes on your database. Debezium supports many relational and non-relational databases. It can report any big data storage systems, like Azure SQL, Big data etc. There are following supported database list which works with Debezium are:
1. MySQL
2. SQL Server
3. Oracle DB
4. DB2
5. MongoDB
6. Cassandra
7. PostgreSQL
8. Vitess

# Consume Change Data
To consume change data based on your own need, may vary need basis. We are explaining two different mechanism that you can use to consume change data in SQL server.
## 1. Writing a custom C# code (Pull method)

## 2. Debezium (Push method)

### Debezium In Action
Debezium is a set of distributed services to capture changes in your databases so that your applications can see those changes and respond to them. Debezium records all row-level changes within each database table in a change event stream, and applications simply read these streams to see the change events in the same order in which they occurred. For more info follow [Debezium doc](https://debezium.io/documentation/reference/2.5/index.html).

In order to bring Debezium in action you need following services.
1. **Zookeeper**:
   ZooKeeper is used in distributed systems for service synchronization and as a naming registry.  When working with Apache Kafka, ZooKeeper is primarily used to track the status of nodes in the Kafka cluster and maintain a list of Kafka topics and messages. 
   
3. **Kafka**:
   Apache Kafka is a distributed event store and stream-processing open-source platform. 

ZooKeeper isn’t memory intensive when it’s working solely with Kafka. Much like memory,  ZooKeeper doesn’t consume CPU resources heavily.  However, it is best practice to provide a dedicated CPU core for ZooKeeper to ensure there are no issues with context switching.

With the popular gain of docker, to spin such systems is become easy and don't have maintain dedicated systems to spin those services. This is the one of the cost effective solution that uses docker configuration to spin container and utilize for our need. Below is the docker-compose file to run the containers and establish communication between each other.

**docker-compose.yaml**
```
version: '3'
services:
  # Zookeeper, single node
  zookeeper:
    image: wurstmeister/zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - 2181:2181
      - 2888:2888
      - 3888:3888

  # kafka single node     
  kafka:
    image: wurstmeister/kafka:latest
    restart: "no"
    links:
      - zookeeper
    ports:
      - 9992:9992
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_LISTENERS: INTERNAL://:29092,EXTERNAL://:9992
      KAFKA_ADVERTISED_LISTENERS: INTERNAL://kafka:29092,EXTERNAL://localhost:9992
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: INTERNAL

  #kafdrop for topic/msg visualization
  kafdrop:
    image: obsidiandynamics/kafdrop
    restart: "no"
    environment:
      KAFKA_BROKERCONNECT: "kafka:29092"
    ports:
      - 9010:9000
    depends_on:
      - kafka

  #Refer https://hub.docker.com/r/debezium/example-postgres/tags?page=1&name=1.9 for basic idea
  sqlserverdebezium:
    image: quay.io/debezium/connect:latest
    links:
     - kafka
    ports:
      - 8083:8083
    environment:
     - BOOTSTRAP_SERVERS=kafka:29092
     - GROUP_ID=1
     - CONFIG_STORAGE_TOPIC=my_connect_configs
     - OFFSET_STORAGE_TOPIC=my_connect_offsets
     - STATUS_STORAGE_TOPIC=my_connect_statuses
    volumes:
     #https://debezium.io/documentation/reference/2.5/connectors/sqlserver.html
     - ./connectors/debezium-connector-sqlserver:/kafka/connect/debezium-connector-sqlserver #first part (before colon), it is the local directory relative connector path of your docker-compose
```
To spin container from above docker-compose.yml file use ```docker-compose up -d``` command to execute from your terminal.

If all goes well you can see all containers running successfully and looks like almost similar to below screen. If you don't have Docker for Windows, then you need one that can download when you [click here](https://docs.docker.com/desktop/install/windows-install/).

![image](https://github.com/rajeesing/cdc/assets/7796293/19af9357-159c-4245-b5b7-cf1ca7da6403)

Once all services are running, this is the time to configure connector. We are using Microsoft SQL connector which you can download when you [click here](https://repo1.maven.org/maven2/io/debezium/debezium-connector-sqlserver/2.6.0.Final/debezium-connector-sqlserver-2.6.0.Final-plugin.tar.gz)

### Configure the Connector
To configure connector, you can use any API client tool for ex. Postman, Bruno etc. and configure the endpoint with below given details:
POST - ```http://localhost:8083/connectors```
```
{
    "name": "cdcexample-connector",
    "config": {
        "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
        "database.hostname": "<Sql Server Name>",
        "database.port": "1433",
        "database.user": "<Sql Server User Name>",
        "database.password": "<Password>",
        "database.dbname": "cdcexample",
        "database.names": "cdcexample",
        "database.server.name": "<Sql Server Name>",
        "table.include.list": "<Table Name>", //in case of multiple, you separate them with comma
        "schema.history.internal.kafka.bootstrap.servers": "kafka:29092",
        "schema.history.internal.kafka.topic": "schemahistory.fullfillment",
        "topic.prefix": "cdcexample",
        "database.trustServerCertificate": true //not suggested for production envs.
    }
}
```


