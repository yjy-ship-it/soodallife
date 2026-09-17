using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpRoomProviderAdviceLimitsV164 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_help_room_entries_help_post_id_provider_profile_id_created_at",
                table: "help_room_entries",
                columns: new[] { "help_post_id", "provider_profile_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_help_room_entries_help_post_id_provider_profile_id_created_at",
                table: "help_room_entries");
        }
    }
}
