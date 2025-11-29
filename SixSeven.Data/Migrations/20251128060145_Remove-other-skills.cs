using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SixSeven.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]

    public partial class Removeotherskills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtherSkills",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "OtherSkills",
                table: "UserProfiles",
                type: "text[]",
                nullable: false);
        }
    }
}
