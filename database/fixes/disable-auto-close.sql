-- Keep the application's SQL cache warm between connections.
-- Run against the development instance hosting JobTot. Does not change business data.
USE [master];
GO
ALTER DATABASE [JobTot] SET AUTO_CLOSE OFF;
GO
SELECT name, is_auto_close_on FROM sys.databases WHERE name = N'JobTot';
