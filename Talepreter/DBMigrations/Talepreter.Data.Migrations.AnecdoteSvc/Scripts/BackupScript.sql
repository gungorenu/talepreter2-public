CREATE PROCEDURE dbo.TPBACKUPTOVERSION @taleId UNIQUEIDENTIFIER, @sourceVersionId UNIQUEIDENTIFIER, @targetVersionId UNIQUEIDENTIFIER
AS

-- Triggers table

INSERT INTO [dbo].[Triggers] ([Id], [TaleId], [TaleVersionId] , [LastUpdate], [State], [TriggerAt], [Target], [GrainType], [GrainId], [Type], [Parameter])
SELECT                        [Id], [TaleId], @targetVersionId, [LastUpdate], [State], [TriggerAt], [Target], [GrainType], [GrainId], [Type], [Parameter] FROM [dbo].[Triggers]
WHERE [TaleId] = @taleId AND [TaleVersionId] = @sourceVersionId;

GO
