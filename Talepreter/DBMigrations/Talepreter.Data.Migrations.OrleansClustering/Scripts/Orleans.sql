-- SHARED source: https://github.com/dotnet/orleans/blob/main/src/AdoNet/Shared/SQLServer-Main.sql

/*
Implementation notes:

1) The general idea is that data is read and written through Orleans specific queries.
   Orleans operates on column names and types when reading and on parameter names and types when writing.

2) The implementations *must* preserve input and output names and types. Orleans uses these parameters to reads query results by name and type.
   Vendor and deployment specific tuning is allowed and contributions are encouraged as long as the interface contract
   is maintained.

3) The implementation across vendor specific scripts *should* preserve the constraint names. This simplifies troubleshooting
   by virtue of uniform naming across concrete implementations.

5) ETag for Orleans is an opaque column that represents a unique version. The type of its actual implementation
   is not important as long as it represents a unique version. In this implementation we use integers for versioning

6) For the sake of being explicit and removing ambiguity, Orleans expects some queries to return either TRUE as >0 value
   or FALSE as =0 value. That is, affected rows or such does not matter. If an error is raised or an exception is thrown
   the query *must* ensure the entire transaction is rolled back and may either return FALSE or propagate the exception.
   Orleans handles exception as a failure and will retry.

7) The implementation follows the Extended Orleans membership protocol. For more information, see at:
        https://learn.microsoft.com/dotnet/orleans/implementation/cluster-management
        https://github.com/dotnet/orleans/blob/main/src/Orleans.Core/SystemTargetInterfaces/IMembershipTable.cs
*/

-- These settings improves throughput of the database by reducing locking by better separating readers from writers.
-- SQL Server 2012 and newer can refer to itself as CURRENT. Older ones need a workaround.
DECLARE @current NVARCHAR(256);
DECLARE @snapshotSettings NVARCHAR(612);

SELECT @current = N'[' + (SELECT DB_NAME()) + N']';
SET @snapshotSettings = N'ALTER DATABASE ' + @current + N' SET READ_COMMITTED_SNAPSHOT ON; ALTER DATABASE ' + @current + N' SET ALLOW_SNAPSHOT_ISOLATION ON;';

EXECUTE sp_executesql @snapshotSettings;

-- This table defines Orleans operational queries. Orleans uses these to manage its operations,
-- these are the only queries Orleans issues to the database.
-- These can be redefined (e.g. to provide non-destructive updates) provided the stated interface principles hold.
IF OBJECT_ID(N'[OrleansQuery]', 'U') IS NULL
CREATE TABLE OrleansQuery
(
	QueryKey VARCHAR(64) NOT NULL,
	QueryText VARCHAR(8000) NOT NULL,

	CONSTRAINT OrleansQuery_Key PRIMARY KEY(QueryKey)
);

-- --------------------------------------------------------------------------------------------------------------------
-- CLUSTERING source: https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Clustering.AdoNet/SQLServer-Clustering.sql

-- For each deployment, there will be only one (active) membership version table version column which will be updated periodically.
IF OBJECT_ID(N'[OrleansMembershipVersionTable]', 'U') IS NULL
CREATE TABLE OrleansMembershipVersionTable
(
	DeploymentId NVARCHAR(150) NOT NULL,
	Timestamp DATETIME2(3) NOT NULL DEFAULT GETUTCDATE(),
	Version INT NOT NULL DEFAULT 0,

	CONSTRAINT PK_OrleansMembershipVersionTable_DeploymentId PRIMARY KEY(DeploymentId)
);

-- Every silo instance has a row in the membership table.
IF OBJECT_ID(N'[OrleansMembershipTable]', 'U') IS NULL
CREATE TABLE OrleansMembershipTable
(
	DeploymentId NVARCHAR(150) NOT NULL,
	Address VARCHAR(45) NOT NULL,
	Port INT NOT NULL,
	Generation INT NOT NULL,
	SiloName NVARCHAR(150) NOT NULL,
	HostName NVARCHAR(150) NOT NULL,
	Status INT NOT NULL,
	ProxyPort INT NULL,
	SuspectTimes VARCHAR(8000) NULL,
	StartTime DATETIME2(3) NOT NULL,
	IAmAliveTime DATETIME2(3) NOT NULL,

	CONSTRAINT PK_MembershipTable_DeploymentId PRIMARY KEY(DeploymentId, Address, Port, Generation),
	CONSTRAINT FK_MembershipTable_MembershipVersionTable_DeploymentId FOREIGN KEY (DeploymentId) REFERENCES OrleansMembershipVersionTable (DeploymentId)
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
	'UpdateIAmAlivetimeKey',
	'-- This is expected to never fail by Orleans, so return value
	-- is not needed nor is it checked.
	SET NOCOUNT ON;
	UPDATE OrleansMembershipTable
	SET
		IAmAliveTime = @IAmAliveTime
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Address = @Address AND @Address IS NOT NULL
		AND Port = @Port AND @Port IS NOT NULL
		AND Generation = @Generation AND @Generation IS NOT NULL;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'UpdateIAmAlivetimeKey'
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT 
	'InsertMembershipVersionKey',
	'SET NOCOUNT ON;
	INSERT INTO OrleansMembershipVersionTable
	(
		DeploymentId
	)
	SELECT @DeploymentId
	WHERE NOT EXISTS
	(
		SELECT 1
		FROM
			OrleansMembershipVersionTable WITH(HOLDLOCK, XLOCK, ROWLOCK)
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
	);
	
	SELECT @@ROWCOUNT;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'InsertMembershipVersionKey'
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
	'InsertMembershipKey',
	'SET XACT_ABORT, NOCOUNT ON;
	DECLARE @ROWCOUNT AS INT;
	BEGIN TRANSACTION;
	INSERT INTO OrleansMembershipTable
	(
		DeploymentId,
		Address,
		Port,
		Generation,
		SiloName,
		HostName,
		Status,
		ProxyPort,
		StartTime,
		IAmAliveTime
	)
	SELECT
		@DeploymentId,
		@Address,
		@Port,
		@Generation,
		@SiloName,
		@HostName,
		@Status,
		@ProxyPort,
		@StartTime,
		@IAmAliveTime
	WHERE NOT EXISTS
	(
		SELECT 1
		FROM
			OrleansMembershipTable WITH(HOLDLOCK, XLOCK, ROWLOCK)
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
			AND Address = @Address AND @Address IS NOT NULL
			AND Port = @Port AND @Port IS NOT NULL
			AND Generation = @Generation AND @Generation IS NOT NULL
	);

	UPDATE OrleansMembershipVersionTable
	SET
		Timestamp = GETUTCDATE(),
		Version = Version + 1
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Version = @Version AND @Version IS NOT NULL
		AND @@ROWCOUNT > 0;
	
	SET @ROWCOUNT = @@ROWCOUNT;
	
	IF @ROWCOUNT = 0
		ROLLBACK TRANSACTION
	ELSE
		COMMIT TRANSACTION
	SELECT @ROWCOUNT;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'InsertMembershipKey'
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
	'UpdateMembershipKey',
	'SET XACT_ABORT, NOCOUNT ON;
	BEGIN TRANSACTION;
	
	UPDATE OrleansMembershipVersionTable
	SET
		Timestamp = GETUTCDATE(),
		Version = Version + 1
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Version = @Version AND @Version IS NOT NULL;
	
	UPDATE OrleansMembershipTable
	SET
		Status = @Status,
		SuspectTimes = @SuspectTimes,
		IAmAliveTime = @IAmAliveTime
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Address = @Address AND @Address IS NOT NULL
		AND Port = @Port AND @Port IS NOT NULL
		AND Generation = @Generation AND @Generation IS NOT NULL
		AND @@ROWCOUNT > 0;
	
	SELECT @@ROWCOUNT;
	COMMIT TRANSACTION;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'UpdateMembershipKey'
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
	'GatewaysQueryKey',
	'SELECT
		Address,
		ProxyPort,
		Generation
	FROM
		OrleansMembershipTable
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Status = @Status AND @Status IS NOT NULL
		AND ProxyPort > 0;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'GatewaysQueryKey'
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
	'MembershipReadRowKey',
	'SELECT
		v.DeploymentId,
		m.Address,
		m.Port,
		m.Generation,
		m.SiloName,
		m.HostName,
		m.Status,
		m.ProxyPort,
		m.SuspectTimes,
		m.StartTime,
		m.IAmAliveTime,
		v.Version
	FROM
		OrleansMembershipVersionTable v
		-- This ensures the version table will returned even if there is no matching membership row.
		LEFT OUTER JOIN OrleansMembershipTable m ON v.DeploymentId = m.DeploymentId
		AND Address = @Address AND @Address IS NOT NULL
		AND Port = @Port AND @Port IS NOT NULL
		AND Generation = @Generation AND @Generation IS NOT NULL
	WHERE
		v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'MembershipReadRowKey'
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
	'MembershipReadAllKey',
	'SELECT
		v.DeploymentId,
		m.Address,
		m.Port,
		m.Generation,
		m.SiloName,
		m.HostName,
		m.Status,
		m.ProxyPort,
		m.SuspectTimes,
		m.StartTime,
		m.IAmAliveTime,
		v.Version
	FROM
		OrleansMembershipVersionTable v LEFT OUTER JOIN OrleansMembershipTable m
		ON v.DeploymentId = m.DeploymentId
	WHERE
		v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'MembershipReadAllKey'
);

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
	'DeleteMembershipTableEntriesKey',
	'DELETE FROM OrleansMembershipTable
	WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	DELETE FROM OrleansMembershipVersionTable
	WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	'
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'DeleteMembershipTableEntriesKey'
);

-- --------------------------------------------------------------------------------------------------------------------
-- CLUSTERING MIGRATION source: https://github.com/dotnet/orleans/blob/main/src/AdoNet/Orleans.Clustering.AdoNet/Migrations/SQLServer-Clustering-3.7.0.sql

INSERT INTO OrleansQuery(QueryKey, QueryText)
SELECT
    'CleanupDefunctSiloEntriesKey',
    'DELETE FROM OrleansMembershipTable
    WHERE DeploymentId = @DeploymentId
        AND @DeploymentId IS NOT NULL
        AND IAmAliveTime < @IAmAliveTime
        AND Status != 3;
    '
WHERE NOT EXISTS 
( 
    SELECT 1 
    FROM OrleansQuery oqt
    WHERE oqt.[QueryKey] = 'CleanupDefunctSiloEntriesKey'
);