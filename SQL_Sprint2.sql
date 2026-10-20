USE [metrixvoxAPI];
GO

-- Agregar columna Weight (peso) a SurveyQuestions
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SurveyQuestions' AND COLUMN_NAME = 'Weight')
BEGIN
    ALTER TABLE SurveyQuestions ADD Weight FLOAT NOT NULL DEFAULT 1.0;
    PRINT '✅ Columna Weight agregada';
END
GO

-- Agregar columna Options (JSON con las opciones para opción múltiple)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SurveyQuestions' AND COLUMN_NAME = 'OptionsJson')
BEGIN
    ALTER TABLE SurveyQuestions ADD OptionsJson NVARCHAR(MAX) NULL;
    PRINT '✅ Columna OptionsJson agregada';
END
GO

-- Actualizar las preguntas existentes para que tengan Weight = 1
UPDATE SurveyQuestions SET Weight = 1.0 WHERE Weight IS NULL;
GO
