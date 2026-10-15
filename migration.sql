IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AiInsights] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] nvarchar(64) NOT NULL,
    [SurveyResponseRecordId] uniqueidentifier NULL,
    [DetectedCategory] nvarchar(100) NOT NULL,
    [VerbatimSnippet] nvarchar(300) NOT NULL,
    [SummaryDescription] nvarchar(500) NOT NULL,
    [IsResolved] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AiInsights] PRIMARY KEY ([Id])
);

CREATE TABLE [SurveyDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] nvarchar(64) NOT NULL,
    [SurveyCode] nvarchar(50) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [IndustryType] nvarchar(50) NOT NULL,
    [Scope] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SurveyDefinitions] PRIMARY KEY ([Id])
);

CREATE TABLE [SurveyResponseRecords] (
    [Id] uniqueidentifier NOT NULL,
    [SurveyCode] nvarchar(50) NOT NULL,
    [TenantId] nvarchar(64) NOT NULL,
    [SubjectId] nvarchar(100) NULL,
    [BranchOrLocationId] nvarchar(100) NOT NULL,
    [Channel] nvarchar(30) NOT NULL,
    [GlobalCalculatedScore] float NOT NULL,
    [Verbatim] nvarchar(2000) NULL,
    [Sentiment] nvarchar(20) NULL,
    [RawPayloadJson] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SurveyResponseRecords] PRIMARY KEY ([Id])
);

CREATE TABLE [SurveySections] (
    [Id] uniqueidentifier NOT NULL,
    [SurveyDefinitionId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Order] int NOT NULL,
    CONSTRAINT [PK_SurveySections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SurveySections_SurveyDefinitions_SurveyDefinitionId] FOREIGN KEY ([SurveyDefinitionId]) REFERENCES [SurveyDefinitions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MetricValueRecords] (
    [Id] uniqueidentifier NOT NULL,
    [SurveyResponseRecordId] uniqueidentifier NOT NULL,
    [Category] nvarchar(200) NOT NULL,
    [MetricKey] nvarchar(100) NOT NULL,
    [NumericValue] float NOT NULL,
    [Weight] float NOT NULL,
    CONSTRAINT [PK_MetricValueRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MetricValueRecords_SurveyResponseRecords_SurveyResponseRecordId] FOREIGN KEY ([SurveyResponseRecordId]) REFERENCES [SurveyResponseRecords] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SurveyQuestions] (
    [Id] uniqueidentifier NOT NULL,
    [SurveySectionId] uniqueidentifier NOT NULL,
    [Text] nvarchar(500) NOT NULL,
    [QuestionType] nvarchar(30) NOT NULL,
    [IsRequired] bit NOT NULL,
    [Order] int NOT NULL,
    CONSTRAINT [PK_SurveyQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SurveyQuestions_SurveySections_SurveySectionId] FOREIGN KEY ([SurveySectionId]) REFERENCES [SurveySections] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AiInsights_TenantId_IsResolved_CreatedAt] ON [AiInsights] ([TenantId], [IsResolved], [CreatedAt]);

CREATE INDEX [IX_MetricValueRecords_SurveyResponseRecordId] ON [MetricValueRecords] ([SurveyResponseRecordId]);

CREATE UNIQUE INDEX [IX_SurveyDefinitions_TenantId_SurveyCode] ON [SurveyDefinitions] ([TenantId], [SurveyCode]);

CREATE INDEX [IX_SurveyQuestions_SurveySectionId] ON [SurveyQuestions] ([SurveySectionId]);

CREATE INDEX [IX_SurveyResponseRecords_TenantId_CreatedAt] ON [SurveyResponseRecords] ([TenantId], [CreatedAt]);

CREATE INDEX [IX_SurveySections_SurveyDefinitionId] ON [SurveySections] ([SurveyDefinitionId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261014195653_InitialCreate', N'10.0.11');

COMMIT;
GO

