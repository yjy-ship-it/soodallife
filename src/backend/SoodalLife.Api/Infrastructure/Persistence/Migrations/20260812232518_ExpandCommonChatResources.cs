using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCommonChatResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_chat_rooms_resource_type",
                table: "chat_rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_chat_rooms_room_type",
                table: "chat_rooms");

            migrationBuilder.AddCheckConstraint(
                name: "CK_chat_rooms_resource_type",
                table: "chat_rooms",
                sql: "[resource_type] IN ('TRANSACTION','SUBSCRIPTION','INTERIOR','AFTER_SERVICE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_chat_rooms_room_type",
                table: "chat_rooms",
                sql: "[room_type] IN ('DIRECT','PRIMARY_CONTRACTOR','SITE_SURVEY')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_chat_rooms_resource_type",
                table: "chat_rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_chat_rooms_room_type",
                table: "chat_rooms");

            migrationBuilder.AddCheckConstraint(
                name: "CK_chat_rooms_resource_type",
                table: "chat_rooms",
                sql: "[resource_type] IN ('TRANSACTION')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_chat_rooms_room_type",
                table: "chat_rooms",
                sql: "[room_type] IN ('DIRECT')");
        }
    }
}
