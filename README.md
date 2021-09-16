# Jocys.com SQL Propagate

Allows quick execution of multiple SQL scripts with paramters on multiple SQL servers.

# Download

Digitally Signed Application v1.0.6.0 (2021-09-16)

[Download - JocysCom.Sql.Propagate.zip](https://github.com/JocysCom/SqlPropagate/releases/download/1.0.0.0/JocysCom.Sql.Propagate.zip) - for All games

## Screenshots

### Files

Program settings are automatically created with the same name as executable, but with \*.xml extension.

<img alt="Files" src="SqlPropagate/Documents/Images/JocysComSqlPropagate_Files.png" width="200" height="70">

### Main Program

You can edit order (#) of execution, choose connections, parameters and scripts to use by simply checking or unchecking items.

<img alt="Main From" src="SqlPropagate/Documents/Images/JocysComSqlPropagate.png" width="700" height="480">

Note: Messages can be reported to the log panel by raising warnings:

```SQL
USE [Database1]
UPDATE Table1 SET Column1 = '$(MyParam1)'
GO

USE [Database2]
UPDATE Table2 SET Column1 = '$(MyParam1)'
GO

DECLARE @error sysname = 'Test Message from Script 1. MyParam1 = ''$(MyParam1)'''
RAISERROR(@error, -1, -1) WITH NOWAIT
```
