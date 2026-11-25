-- Adds one private AI history table. Does not alter existing messages or account data.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.AiChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiChatMessages (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AiChatMessages PRIMARY KEY,
        AccountId uniqueidentifier NOT NULL,
        ConversationId uniqueidentifier NOT NULL,
        TurnId uniqueidentifier NOT NULL,
        Role nvarchar(16) NOT NULL,
        Content nvarchar(max) NOT NULL,
        State nvarchar(16) NOT NULL,
        ImageName nvarchar(80) NULL,
        ImageMimeType nvarchar(32) NULL,
        CreatedAt datetimeoffset NOT NULL,
        UpdatedAt datetimeoffset NOT NULL,
        CONSTRAINT FK_AiChatMessages_Account_AccountId FOREIGN KEY(AccountId) REFERENCES dbo.Account(Id)
    );
END;
IF COL_LENGTH(N'dbo.AiChatMessages', N'TurnId') IS NULL
    THROW 51000, 'Existing AI history table does not match the migration.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AiChatMessages') AND name=N'IX_AiChatMessages_AccountId_ConversationId_Id')
    CREATE INDEX IX_AiChatMessages_AccountId_ConversationId_Id ON dbo.AiChatMessages(AccountId,ConversationId,Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AiChatMessages') AND name=N'IX_AiChatMessages_TurnId_Role')
    CREATE UNIQUE INDEX IX_AiChatMessages_TurnId_Role ON dbo.AiChatMessages(TurnId,Role);
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261125010000_AddAiChatHistory')
    INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20261125010000_AddAiChatHistory',N'9.0.9');
COMMIT;
SELECT COUNT(*) AS AiHistoryRows FROM dbo.AiChatMessages;
