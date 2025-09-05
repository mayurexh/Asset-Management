using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asset_Management.Migrations
{
    /// <inheritdoc />
    public partial class DateTimeInAssetLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LogTime",
                table: "AssetLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogTime",
                table: "AssetLogs");
        }
    }
}
