-- Demo: Read SQL Server version details and echo a custom message.
-- Parameter: $(MyParam1) - custom message to echo back.
-- This script is read-only and safe to run on any server.

-- Echo custom message.
DECLARE @msg nvarchar(256) = '$(MyParam1)'
RAISERROR(@msg, -1, -1) WITH NOWAIT
GO

-- Server edition.
SELECT
    SERVERPROPERTY('MachineName')        AS [MachineName],
    SERVERPROPERTY('ServerName')         AS [ServerName],
    SERVERPROPERTY('Edition')            AS [Edition]
