-- Replace stale demo-company suffixes with each job's current company display name.
-- Keeps original titles in a backup table; safe to rerun.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.JobTitleCompanyRenameBackup', N'U') IS NULL
    CREATE TABLE dbo.JobTitleCompanyRenameBackup (
        JobId uniqueidentifier NOT NULL,
        OldTitle nvarchar(200) NOT NULL,
        NewTitle nvarchar(200) NOT NULL,
        ChangedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
SELECT j.Id, j.Title AS OldTitle,
    LEFT(j.Title, CHARINDEX(N' - Demo ', j.Title) - 1) + N' - ' + c.DisplayName AS NewTitle
INTO #TitleChanges
FROM dbo.Jobs j WITH (UPDLOCK, HOLDLOCK)
JOIN dbo.Companies c ON c.Id = j.CompanyId
WHERE CHARINDEX(N' - Demo ', j.Title) > 0
  AND NULLIF(LTRIM(RTRIM(c.DisplayName)), N'') IS NOT NULL
  AND c.DisplayName NOT LIKE N'%Demo%';
IF EXISTS (SELECT 1 FROM #TitleChanges WHERE LEN(NewTitle) > 200)
    THROW 51000, 'A replacement job title exceeds 200 characters; no changes applied.', 1;
INSERT dbo.JobTitleCompanyRenameBackup (JobId, OldTitle, NewTitle)
SELECT Id, OldTitle, NewTitle FROM #TitleChanges;
UPDATE j SET Title = t.NewTitle
FROM dbo.Jobs j JOIN #TitleChanges t ON j.Id = t.Id;
SELECT @@ROWCOUNT AS UpdatedJobTitles;
IF EXISTS (SELECT 1 FROM dbo.Jobs j JOIN #TitleChanges t ON j.Id=t.Id WHERE j.Title<>t.NewTitle)
    THROW 51001, 'Job title verification failed.', 1;
COMMIT TRANSACTION;
SELECT COUNT(*) AS RemainingDemoJobTitles FROM dbo.Jobs WHERE Title LIKE N'%Demo%';