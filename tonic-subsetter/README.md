# 1. Subsetter Services

<!-- TOC -->
- [1.1. Web Service](#11-web-service)
  - [1.1.1. Job Endpoints](#111-job-endpoints)
  - [1.1.2. Status Endpoints](#112-status-endpoints)
- [1.2. Clean Batch](#12-clean-batch)
- [1.3. Foreign Key Mask Configure](#13-foreign-key-mask-configure)
- [1.4. Primary Key Remapper](#14-primary-key-remapper)
- [1.5. Reset Schema](#15-reset-schema)
- [1.6. Subsetter](#16-subsetter)
- [1.7. Table Count](#17-table-count)
- [1.8. Adding a new tool](#18-adding-a-new-tool)
  - [1.8.1. Files](#181-files)
  - [1.8.2. ECR Repository](#182-ecr-repository)
<!-- /TOC -->

## 1.1. Web Service

The web service provides endpoints for each of the following tools to run in a test environment. It provides the following
endpoints.

### 1.1.1. Job Endpoints

Each of these endpoints kicks off a job for the requested service. If successful, they return a 200 and a JSON response
roughly resembling the following:

```json
  {
    "status": {
      "statusCode": 200,
      "message": "[Service Name] task started",
      "systemTime": 1596139935151
    },
    "results": {
      "jobId": "9e52e547-8a44-4a51-88ca-8456e3e93539",
      "jobStatus": "Successful",
      "jobResult": null
    }
  }
```

- `HTTP POST /schemas/reset`: Takes an `application/toml` body containing the concatenated TOML configuration files
  for the various services in this repository, most importantly the main subset configuration, and starts a Reset Schema
  job on the destination DB(s) in the configuration.
- `HTTP POST /tables/count`: Takes an `application/toml` body containing the concatenated TOML configuration files
  for the various services in this repository, most importantly the main subset configuration. Returns a dictionary of
  table counts of the destination in the `jobResult` field, which must be retrieved by a `/jobs/status/[jobID]` call, for example:

```json
  {
    "status": {
      "statusCode": 200,
      "message": "[Service Name] task started",
      "systemTime": 1596139935151
    },
    "results": {
      "jobId": "9e52e547-8a44-4a51-88ca-8456e3e93539",
      "jobStatus": "Successful",
      "jobResult": {
          "SCHEMA.TABLE_NAME": "1001"
      }
    }
  }
```

- `HTTP POST /subsetter`: Takes an `application/toml` body containing the concatenated TOML configuration files
  for the various services in this repository, most importantly the main subset configuration. Starts a Subsetter job
  based on the supplied configuration supplied.

- `HTTP POST /foreign_keys/configure_mask`: Takes an `application/toml` body containing the concatenated TOML configuration files
  for the various services in this repository, most importantly the foreign keys configuration and the settings for the foreign key mask configuration.
  It adds integer key masks to each of the specified foreign keys in the configuration to the respective workspaces in Tonic. Returns true if successful,
  false if unsuccessful.

- `HTTP POST /primary_keys/remap`: Takes an `application/toml` body containing the concatenated TOML configuration files
  for the various services in this repository, most importantly the main subset configuration. Remaps the primary keys, and
  returns a dictionary containing all of those remapped IDs for post-processing. For example:

```json
  {
    "status": {
      "statusCode": 200,
      "message": "[Service Name] task started",
      "systemTime": 1596139935151
    },
    "results": {
      "jobId": "9e52e547-8a44-4a51-88ca-8456e3e93539",
      "jobStatus": "Successful",
      "jobResult": {
          "TABLE_NAME": {
              "COLUMN_NAME": [
                  "123",
                  "456",
                  "789"
              ]
          }
      }
    }
  }
```

### 1.1.2. Status Endpoints

There are two endpoints to retrieve the status of a job, which ultimately carry job results for the PK Remapper and Table Count.

- `HTTP GET /jobs/status`: Gets the current status and return values for all jobs that haven't been trashed (currently every 5 days).
  Returns the same output as the above endpoints do, but returns them as a list.

- `HTTP GET/jobs/status/[jobID]`: Gets the current status for a single job, as a simple Object in JSON like the above responses.

## 1.2. Clean Batch

This tools removes all records inserted in Staging by the Primary Key Remapper tool. Can be configured to use either/both BatchId's
or individual Id's as passed to the `Subsetter`:
```
[CleanBatchOptions]
DryRun = true
BatchIds = [100] # will clean all rows from ReuseRows / Staging where batchid == 100

[[ImportIds]] # note: column key names are identical to those passed in `Subsetter` configs - must also include [[DirectTarget]] config
<Column Key Name> = [999] # will clean all rows whose batchd == (batchid of item with id column == 999)
```

## 1.3. Foreign Key Mask Configure

This tool uses the foreign keys specified in the TOML configuration and adds masking to their respective columns within
the Tonic workspace (saving manual work and error).

## 1.4. Primary Key Remapper

This tool remaps masked primary keys, returning or writing to disk a set of masked IDs that correspond to production IDs.

## 1.5. Reset Schema

This tool resets the schema on the destination database by dropping all unnecessary tables, and recreating the schema
necessary for subsetting based on the configuration of targets and hosts in the subset configuration TOML file.
After resetting the specified schemas, a post script stage is run targeting the TONICP databases

Please be careful with the configuration here! You wouldn't want to drop a production database :).

## 1.6. Subsetter

This tool creates a subset of the various hosts and targets based on various IDs and their foreign table dependencies.
It uses the same configuration as the reset schema to determine logical hosts, targets, and the IDs to use for seeding
the initial subset, then crawls the various databases to create a single set of output databases.

If there are many logical hosts for a type (say USER_[0 - 10]), they will all be consolidated into a subset form in the
USER_0 table on the output.

## 1.7. Table Count

This tool uses the subset configuration to display the row count across all of tables in the destination DB related to the
subset.

## 1.8. Adding a new tool

### 1.8.1. Files

Modified the following files to add support for a new tool:

- `Tonic.sln`
- `Dockerfile`
- `Makefile`
- `README.md`
- `Jenkinsfile.build-docker-image`
- `jenkins-jobs/jobs/tonic_subsetter_build_docker_image.groovy`

### 1.8.2. ECR Repository

Create a _public_ repository in ECR with the following settings:

- *Name:* tonic-_[NAME]_
- *Description:* Fix Staging Data - Tonic._[NAME]_ tool.
- In _Repository Settings_:

  - Add _users_ team with _read_ permission
  - Add _staginginitiative+jenkins_  robot account with _write_ permission

# 2. Configuring Options
## 2.1 Reset Schema Options
### 2.1.1 Truncate Only Mode
Directs `Resetschema` to truncate existing tables, does not remap schema from source.
```toml
[[ResetSchemaOptions.TableOptions]]
TruncateOnly =  true
```
## 2.2 Subsetter
### 2.2.1 Detailed Subsetter Information
To print detailed subsetter information (listing the upstream/downstream relationships of the subsetted tables), add to the subsetter options 
```toml
[SubsetOptons]
# ...
DetailedSubsetInformation = true
```
## 2.3 Primary Key Remapper Options
### 2.3.1 Sharded Staging Destination
To configure the PrimaryKeyRemapper to upload masked & remapped rows to different staging users/tables, depending on the modulo value of some column, add the following to the PrimaryKeyRemapper configuration:
```toml
[[DestinationLogicalHost]]
LogicalHostCategory = "Category"
Host = "url"
Port = 1521
User = "user"
Password = "*******"
Sid = "sid"
Schema = "schema"
ShardedIndex = 0 # define the destination host as normal
    # define other staging definitions as children of default host - can override any/all properties of default host
    [[DestinationLogicalHost.DestinationShard]]
    User = "other_user"
    Schema = "other_schema"
    Password = "******"
    ShardedIndex = 1

# definition to map suffixed table -> staging shard
[[DestinationShardedTableMap]]
LogicalHostCategory = "Category"
ShardIndex = 1 # `DestinationLogicalHost.ShardedIndex` value this mapping uses as a staging destination
TABLE_00 = 11 # int value for this destination, e.g. rows where  (col val % modulus = 11) will be sent to ShardedIndex = 1 
SOME_OTHER_SHARDED_TABLE_00 = 11

# define the table column to be used in calculating staging shard
[[ShardedDestinationHostTable]]
LogicalHostCategory = "Category"
Table = "TABLE_00"
Column = "ID"
DataType = "NUMBER" # must use columns where type == "NUMBER"
Modulo = 0 # shardindex to match: columns where val % modulus = 0 will be sent to TABLE_00
Modulus = 12 # required for *_00 tables, the modulus to take the remainder against
[[ShardedDestinationHostTable]]
LogicalHostCategory = "Category"
Table = "TABLE_01"
Column = "ID"
DataType = "NUMBER" # must use columns where type == "NUMBER"
Modulo = 1 # shardindex to match: columns where val % modulus = 1 will be sent to TABLE_01
```

### 2.3.2 Debug ReuseRows Queries 
By default, `PrimaryKeyRemapper` does not log the queries to ReuseRows db. To enable their logging, set to `true` in the options
```toml
[PrimaryKeyRemapperOptions]
# ...
DebugQueries = true
```

### 2.3.3 Replaced Row Key Logging
By default, `PrimaryKeyRemapper` does not record the row keys of records replaced during a run. To record these keys:
```toml
[PrimaryKeyRemapperOptions]
# Replaced row keys will be available in the tonic_pkremapper_<RunId>_replaced_keys.csv file,
# in a `<Table Name>, <Column Name>, <Original Value>, <New Value>, <Batch Id>` csv format (includes column header line)
WriteReplacedKeysToFile = true
```

## 2.3.4 Primary Key Remapper Upsert
During the upload stage of the PrimaryKeyRemapper, the normal behavior bulk uploads the rows to the staging database, using Oracle's sql-loader. An upsert can be performed instead, where rows are uploaded to a temporary table in the staging database. These rows are then joined against the target table on some specified column(s), those rows which match a staging row are instead used to update the target row. Rows not matching any target row are inserted.
To specify a table to be upserted:
```toml
 [[UpsertTable]]
 LogicalHostCategory = "TestHost"
 Table = "TEST_TABLE"
 ColumnNames = ["column_a", "column_b"]
```

## 2.4 Table Count Options
### 2.4.1 Selectively Include Workspaces

By default, `TableCount` operates over every Logical Host Category it detects.
To selectively count tables, two config items must be passed to `TableCount`:
```toml
# A mapping of included workspaces inserted into a toml multi-line string, where each line 
# corresponds to the `alias,user,workspace` of a logical host category to be included
[TableCount.CountOptions]
IncludedWorkspaces='''
alias_a,user_a,workspace a
alias_b,user_b,workspace b'''

# A <key> : <value> mapping of logical host categories to their workspace names.
# Workspace names may be suffixed with `(000)`, or text/digits enclosed by parentheses
[LogicalHostCategoryToWorkspace]
user_a = "workspace a (DR1)"
user_b = "workspace b (008)"
user_c = "workspace c"
```
