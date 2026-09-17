SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.review_comments',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.review_comments(
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_review_comments PRIMARY KEY,
        public_id uniqueidentifier NOT NULL,
        review_id bigint NOT NULL,
        parent_comment_id bigint NULL,
        author_user_id bigint NOT NULL,
        author_role_code varchar(20) NOT NULL,
        author_display_name nvarchar(150) NOT NULL,
        body_text nvarchar(2000) NOT NULL,
        status_code varchar(20) NOT NULL CONSTRAINT DF_review_comments_status DEFAULT('ACTIVE'),
        idempotency_key varchar(150) NOT NULL,
        submitted_at datetime2(7) NOT NULL,
        hidden_at datetime2(7) NULL,
        hidden_by_user_id bigint NULL,
        hidden_reason nvarchar(1000) NULL,
        created_at datetime2(7) NOT NULL CONSTRAINT DF_review_comments_created DEFAULT(SYSUTCDATETIME()),
        updated_at datetime2(7) NOT NULL CONSTRAINT DF_review_comments_updated DEFAULT(SYSUTCDATETIME()),
        row_version rowversion NOT NULL,
        CONSTRAINT CK_review_comments_author_role CHECK(author_role_code IN('CUSTOMER','PROVIDER')),
        CONSTRAINT CK_review_comments_status CHECK(status_code IN('ACTIVE','HIDDEN')),
        CONSTRAINT FK_review_comments_reviews_review_id FOREIGN KEY(review_id) REFERENCES dbo.reviews(id),
        CONSTRAINT FK_review_comments_parent_comment_id FOREIGN KEY(parent_comment_id) REFERENCES dbo.review_comments(id),
        CONSTRAINT FK_review_comments_users_author_user_id FOREIGN KEY(author_user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_review_comments_users_hidden_by_user_id FOREIGN KEY(hidden_by_user_id) REFERENCES dbo.users(id)
    );
    CREATE UNIQUE INDEX IX_review_comments_public_id ON dbo.review_comments(public_id);
    CREATE UNIQUE INDEX IX_review_comments_idempotency_key ON dbo.review_comments(idempotency_key);
    CREATE INDEX IX_review_comments_review_id_submitted_at ON dbo.review_comments(review_id,submitted_at);
    CREATE INDEX IX_review_comments_parent_comment_id ON dbo.review_comments(parent_comment_id);
END;

INSERT dbo.review_comments(public_id,review_id,parent_comment_id,author_user_id,author_role_code,author_display_name,body_text,status_code,idempotency_key,submitted_at,created_at,updated_at)
SELECT r.public_id,r.review_id,NULL,p.user_id,'PROVIDER',p.business_name,r.body_text,'ACTIVE',CONCAT('legacy-provider-reply:',CONVERT(varchar(36),r.public_id)),r.submitted_at,r.created_at,r.updated_at
FROM dbo.review_provider_replies r
JOIN dbo.provider_profiles p ON p.id=r.provider_profile_id
WHERE NOT EXISTS(SELECT 1 FROM dbo.review_comments c WHERE c.idempotency_key=CONCAT('legacy-provider-reply:',CONVERT(varchar(36),r.public_id)));

COMMIT TRANSACTION;
