SELECT @@VERSION

DECLARE @error sysname = 'Test Message from Script 2. MyParam1 = ''$(MyParam1)'''
RAISERROR(@error, -1, -1) WITH NOWAIT
