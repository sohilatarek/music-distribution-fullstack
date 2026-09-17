using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicDistribution.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackListIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tracks_ArtistId",
                table: "Tracks");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_ArtistId_Genre_Status",
                table: "Tracks",
                columns: new[] { "ArtistId", "Genre", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tracks_ArtistId_Genre_Status",
                table: "Tracks");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_ArtistId",
                table: "Tracks",
                column: "ArtistId");
        }
    }
}
