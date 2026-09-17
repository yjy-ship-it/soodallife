using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[Migration("20260824160000_AddGeneralSiteVisitV132")]
public partial class AddGeneralSiteVisitV132:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[site_visit_proposals]', N'U') IS NULL
BEGIN
 CREATE TABLE [dbo].[site_visit_proposals](
  [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_site_visit_proposals] PRIMARY KEY,
  [public_id] uniqueidentifier NOT NULL CONSTRAINT [DF_site_visit_public_id] DEFAULT NEWSEQUENTIALID(),
  [service_request_id] bigint NOT NULL,[request_dispatch_id] bigint NOT NULL,[provider_profile_id] bigint NOT NULL,
  [status_code] varchar(30) NOT NULL CONSTRAINT [DF_site_visit_status] DEFAULT 'PROPOSED',
  [scheduled_at] datetime2(0) NOT NULL,[estimated_duration_minutes] int NOT NULL CONSTRAINT [DF_site_visit_duration] DEFAULT 30,
  [visit_fee_amount] decimal(19,4) NOT NULL,[payment_mode_code] varchar(30) NOT NULL CONSTRAINT [DF_site_visit_payment_mode] DEFAULT 'NO_FEE',
  [payment_status_code] varchar(30) NOT NULL CONSTRAINT [DF_site_visit_payment_status] DEFAULT 'NOT_REQUIRED',
  [deduct_from_work_amount] bit NOT NULL CONSTRAINT [DF_site_visit_deduct] DEFAULT 0,[terms_text] nvarchar(2000) NULL,
  [payment_instruction_protected] nvarchar(2000) NULL,[no_show_wait_minutes] int NOT NULL CONSTRAINT [DF_site_visit_wait] DEFAULT 10,
  [expires_at] datetime2(0) NOT NULL,[accepted_at] datetime2(0) NULL,[payment_reported_at] datetime2(0) NULL,
  [payment_confirmed_at] datetime2(0) NULL,[departed_at] datetime2(0) NULL,[arrived_at] datetime2(0) NULL,[completed_at] datetime2(0) NULL,
  [no_show_status_code] varchar(30) NULL,[no_show_reported_at] datetime2(0) NULL,[idempotency_key] varchar(100) NOT NULL,
  [created_at] datetime2(0) NOT NULL,[created_by_user_id] bigint NULL,[updated_at] datetime2(0) NOT NULL,[updated_by_user_id] bigint NULL,
  [row_version] rowversion NOT NULL,
  CONSTRAINT [FK_site_visit_request] FOREIGN KEY([service_request_id]) REFERENCES [dbo].[service_requests]([id]),
  CONSTRAINT [FK_site_visit_dispatch] FOREIGN KEY([request_dispatch_id]) REFERENCES [dbo].[request_dispatches]([id]),
  CONSTRAINT [FK_site_visit_provider] FOREIGN KEY([provider_profile_id]) REFERENCES [dbo].[provider_profiles]([id]),
  CONSTRAINT [FK_site_visit_created_by] FOREIGN KEY([created_by_user_id]) REFERENCES [dbo].[users]([id]),
  CONSTRAINT [FK_site_visit_updated_by] FOREIGN KEY([updated_by_user_id]) REFERENCES [dbo].[users]([id]),
  CONSTRAINT [CK_site_visit_status] CHECK ([status_code] IN ('PROPOSED','ACCEPTED','DEPARTED','ARRIVED','COMPLETED','REJECTED','CANCELLED','EXPIRED','NO_SHOW','DISPUTED')),
  CONSTRAINT [CK_site_visit_payment_mode] CHECK ([payment_mode_code] IN ('NO_FEE','ON_SITE','TRANSFER_REPORTED','TRANSFER_CONFIRMED')),
  CONSTRAINT [CK_site_visit_payment_status] CHECK ([payment_status_code] IN ('NOT_REQUIRED','ON_SITE_PENDING','AWAITING_TRANSFER','REPORTED','CONFIRMED','REJECTED')),
  CONSTRAINT [CK_site_visit_amount] CHECK ([visit_fee_amount]>=0),CONSTRAINT [CK_site_visit_duration] CHECK ([estimated_duration_minutes] BETWEEN 10 AND 480),
  CONSTRAINT [CK_site_visit_wait] CHECK ([no_show_wait_minutes] BETWEEN 5 AND 60)
 );
 CREATE UNIQUE INDEX [UX_site_visit_public_id] ON [dbo].[site_visit_proposals]([public_id]);
 CREATE UNIQUE INDEX [UX_site_visit_dispatch] ON [dbo].[site_visit_proposals]([request_dispatch_id]);
 CREATE UNIQUE INDEX [UX_site_visit_idempotency] ON [dbo].[site_visit_proposals]([idempotency_key]);
 CREATE INDEX [IX_site_visit_request_status] ON [dbo].[site_visit_proposals]([service_request_id],[status_code]);
END;
IF OBJECT_ID(N'[dbo].[site_visit_events]', N'U') IS NULL
BEGIN
 CREATE TABLE [dbo].[site_visit_events](
  [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_site_visit_events] PRIMARY KEY,[public_id] uniqueidentifier NOT NULL CONSTRAINT [DF_site_visit_event_public_id] DEFAULT NEWSEQUENTIALID(),
  [site_visit_proposal_id] bigint NOT NULL,[actor_user_id] bigint NOT NULL,[event_type_code] varchar(40) NOT NULL,[note] nvarchar(1000) NULL,
  [occurred_at] datetime2(0) NOT NULL,[idempotency_key] varchar(100) NOT NULL,
  CONSTRAINT [FK_site_visit_event_proposal] FOREIGN KEY([site_visit_proposal_id]) REFERENCES [dbo].[site_visit_proposals]([id]),
  CONSTRAINT [FK_site_visit_event_actor] FOREIGN KEY([actor_user_id]) REFERENCES [dbo].[users]([id])
 );
 CREATE UNIQUE INDEX [UX_site_visit_event_public_id] ON [dbo].[site_visit_events]([public_id]);
 CREATE UNIQUE INDEX [UX_site_visit_event_idempotency] ON [dbo].[site_visit_events]([idempotency_key]);
 CREATE INDEX [IX_site_visit_event_timeline] ON [dbo].[site_visit_events]([site_visit_proposal_id],[occurred_at]);
END;
""");
    protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[site_visit_events]',N'U') IS NOT NULL DROP TABLE [dbo].[site_visit_events];
IF OBJECT_ID(N'[dbo].[site_visit_proposals]',N'U') IS NOT NULL DROP TABLE [dbo].[site_visit_proposals];
""");
}
