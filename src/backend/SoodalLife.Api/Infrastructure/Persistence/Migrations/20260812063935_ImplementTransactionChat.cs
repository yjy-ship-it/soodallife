using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementTransactionChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_rooms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    resource_type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    resource_public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    room_type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DIRECT"),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_rooms", x => x.id);
                    table.CheckConstraint("CK_chat_rooms_resource_type", "[resource_type] IN ('TRANSACTION')");
                    table.CheckConstraint("CK_chat_rooms_room_type", "[room_type] IN ('DIRECT')");
                    table.CheckConstraint("CK_chat_rooms_status", "[status_code] IN ('ACTIVE','READ_ONLY','CLOSED')");
                });

            migrationBuilder.CreateTable(
                name: "chat_participants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chat_room_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    participant_role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    joined_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    access_started_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    access_ended_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_participants", x => x.id);
                    table.CheckConstraint("CK_chat_participants_access", "[access_ended_at] IS NULL OR [access_ended_at] >= [access_started_at]");
                    table.CheckConstraint("CK_chat_participants_role", "[participant_role] IN ('CUSTOMER','PROVIDER')");
                    table.CheckConstraint("CK_chat_participants_status", "[status_code] IN ('ACTIVE','ENDED')");
                    table.ForeignKey(
                        name: "FK_chat_participants_chat_rooms_chat_room_id",
                        column: x => x.chat_room_id,
                        principalTable: "chat_rooms",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chat_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chat_room_id = table.Column<long>(type: "bigint", nullable: false),
                    sender_participant_id = table.Column<long>(type: "bigint", nullable: false),
                    message_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    idempotency_key = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.CheckConstraint("CK_chat_messages_body", "([message_type] = 'TEXT' AND [body] IS NOT NULL AND LEN(LTRIM(RTRIM([body]))) > 0) OR [message_type] = 'FILE'");
                    table.CheckConstraint("CK_chat_messages_type", "[message_type] IN ('TEXT','FILE')");
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_participants_sender_participant_id",
                        column: x => x.sender_participant_id,
                        principalTable: "chat_participants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_rooms_chat_room_id",
                        column: x => x.chat_room_id,
                        principalTable: "chat_rooms",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "chat_attachments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chat_message_id = table.Column<long>(type: "bigint", nullable: false),
                    file_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_attachments_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chat_attachments_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chat_attachments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "chat_message_reads",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    chat_room_id = table.Column<long>(type: "bigint", nullable: false),
                    chat_message_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message_reads", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_message_reads_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chat_message_reads_chat_rooms_chat_room_id",
                        column: x => x.chat_room_id,
                        principalTable: "chat_rooms",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_chat_message_reads_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_chat_message_id_file_id",
                table: "chat_attachments",
                columns: new[] { "chat_message_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_created_by_user_id",
                table: "chat_attachments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_file_id",
                table: "chat_attachments",
                column: "file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_public_id",
                table: "chat_attachments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_reads_chat_message_id_user_id",
                table: "chat_message_reads",
                columns: new[] { "chat_message_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_reads_chat_room_id_user_id_read_at",
                table: "chat_message_reads",
                columns: new[] { "chat_room_id", "user_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_reads_user_id",
                table: "chat_message_reads",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_chat_room_id_id",
                table: "chat_messages",
                columns: new[] { "chat_room_id", "id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_idempotency_key",
                table: "chat_messages",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_public_id",
                table: "chat_messages",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_sender_participant_id",
                table: "chat_messages",
                column: "sender_participant_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_chat_room_id_user_id",
                table: "chat_participants",
                columns: new[] { "chat_room_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_public_id",
                table: "chat_participants",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_participants_user_id_status_code",
                table: "chat_participants",
                columns: new[] { "user_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_rooms_public_id",
                table: "chat_rooms",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_rooms_resource_type_resource_public_id_room_type",
                table: "chat_rooms",
                columns: new[] { "resource_type", "resource_public_id", "room_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_rooms_status_code",
                table: "chat_rooms",
                column: "status_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_attachments");

            migrationBuilder.DropTable(
                name: "chat_message_reads");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_participants");

            migrationBuilder.DropTable(
                name: "chat_rooms");
        }
    }
}
