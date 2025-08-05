using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Longhl104.ShelterHub.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePetModel_20250805_221600 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMicrochipped",
                schema: "shelter_hub",
                table: "pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMicrochipped",
                schema: "shelter_hub",
                table: "pets");
        }
    }
}
