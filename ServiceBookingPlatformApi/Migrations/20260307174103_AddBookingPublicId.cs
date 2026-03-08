using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceBookingPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the public-facing GUID identifier column
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            // Enforce uniqueness on the public-facing URL identifier
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PublicId",
                table: "Bookings",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_PublicId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Bookings");
        }
    }
}
