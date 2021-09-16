USE [master]
GO

DECLARE @error sysname
SELECT @error = 'Server Language = ' + @@LANGUAGE
RAISERROR(@error, -1, -1) WITH NOWAIT

GO
DECLARE @error sysname
SELECT @error = 'Message from Script 1. MyParam1 = ''$(MyParam1)''.'
RAISERROR(@error, -1, -1) WITH NOWAIT
