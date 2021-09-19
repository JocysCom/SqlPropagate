# Jocys.com SQL Propagate

Execute SQL scripts with parameters on multiple database server connections. Tool is customisable and can be tailored to specific tasks. You can change title header, body and initial log panel text to provide help and instructions. 

# Download

Digitally Signed Application v1.0.8.0 (2021-09-16)

[Download - JocysCom.Sql.Propagate.zip](https://github.com/JocysCom/SqlPropagate/releases/download/1.0.0.0/JocysCom.Sql.Propagate.zip)

## Screenshots

### Files

Program settings are automatically created with the same name as executable, but with \*.xml extension.

<img alt="Files" src="SqlPropagate/Documents/Images/JocysComSqlPropagate_Files.png" width="200" height="70">

### Main Program

You can edit order (#) of execution, choose connections, parameters and scripts to use by simply checking or unchecking items.
Execution of 2 scripts with 1 parameter on 2 connections:

<img alt="Main From" src="SqlPropagate/Documents/Images/JocysComSqlPropagate.png" width="700" height="480">

Note: Messages can be reported to the log panel by raising warnings:

```SQL
USE [master]
GO

DECLARE @error sysname
SELECT @error = 'Server Language = ' + @@LANGUAGE
RAISERROR(@error, -1, -1) WITH NOWAIT
GO

DECLARE @error sysname = 'Message from Script 1. MyParam1 = ''$(MyParam1)''.'
RAISERROR(@error, -1, -1) WITH NOWAIT
```
