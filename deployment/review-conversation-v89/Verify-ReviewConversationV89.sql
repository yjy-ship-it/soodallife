SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.review_comments',N'U') IS NULL THROW 51000,'review_comments table is missing.',1;
IF COL_LENGTH(N'dbo.review_comments',N'parent_comment_id') IS NULL THROW 51000,'parent_comment_id is missing.',1;
IF COL_LENGTH(N'dbo.review_comments',N'author_role_code') IS NULL THROW 51000,'author_role_code is missing.',1;
IF COL_LENGTH(N'dbo.review_comments',N'hidden_reason') IS NULL THROW 51000,'hidden_reason is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.review_comments') AND name=N'IX_review_comments_idempotency_key' AND is_unique=1) THROW 51000,'review comment idempotency index is missing.',1;
IF EXISTS(SELECT 1 FROM dbo.review_provider_replies r WHERE NOT EXISTS(SELECT 1 FROM dbo.review_comments c WHERE c.idempotency_key=CONCAT('legacy-provider-reply:',CONVERT(varchar(36),r.public_id)))) THROW 51000,'legacy provider reply migration is incomplete.',1;
SELECT COUNT(*) AS ReviewCommentCount FROM dbo.review_comments;
