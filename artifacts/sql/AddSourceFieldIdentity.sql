BEGIN TRANSACTION;
DROP INDEX [IX_category_field_definitions_owner_middle_category_id_field_key] ON [category_field_definitions];

CREATE INDEX [IX_category_field_definitions_owner_middle_category_id_field_key] ON [category_field_definitions] ([owner_middle_category_id], [field_key]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260809121223_AddSourceFieldIdentity', N'10.0.10');

COMMIT;
GO

