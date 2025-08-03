using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Longhl104.ShelterHub.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shelter_hub");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "pet_species",
                schema: "shelter_hub",
                columns: table => new
                {
                    SpeciesId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_species", x => x.SpeciesId);
                });

            migrationBuilder.CreateTable(
                name: "shelters",
                schema: "shelter_hub",
                columns: table => new
                {
                    ShelterId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShelterName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShelterContactNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    ShelterAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<Point>(type: "geometry (point, 4326)", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shelters", x => x.ShelterId);
                });

            migrationBuilder.CreateTable(
                name: "pet_breeds",
                schema: "shelter_hub",
                columns: table => new
                {
                    BreedId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_breeds", x => x.BreedId);
                    table.ForeignKey(
                        name: "FK_pet_breeds_pet_species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalSchema: "shelter_hub",
                        principalTable: "pet_species",
                        principalColumn: "SpeciesId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pets",
                schema: "shelter_hub",
                columns: table => new
                {
                    PetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SpeciesId = table.Column<int>(type: "integer", nullable: false),
                    BreedId = table.Column<int>(type: "integer", nullable: true),
                    ShelterId = table.Column<int>(type: "integer", nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsNeutered = table.Column<bool>(type: "boolean", nullable: false),
                    IsVaccinated = table.Column<bool>(type: "boolean", nullable: false),
                    IsGoodWithKids = table.Column<bool>(type: "boolean", nullable: false),
                    IsGoodWithPets = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pets", x => x.PetId);
                    table.ForeignKey(
                        name: "FK_pets_pet_breeds_BreedId",
                        column: x => x.BreedId,
                        principalSchema: "shelter_hub",
                        principalTable: "pet_breeds",
                        principalColumn: "BreedId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pets_pet_species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalSchema: "shelter_hub",
                        principalTable: "pet_species",
                        principalColumn: "SpeciesId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pets_shelters_ShelterId",
                        column: x => x.ShelterId,
                        principalSchema: "shelter_hub",
                        principalTable: "shelters",
                        principalColumn: "ShelterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "shelter_hub",
                table: "pet_species",
                columns: new[] { "SpeciesId", "Name" },
                values: new object[,]
                {
                    { 1, "Dog" },
                    { 2, "Cat" },
                    { 3, "Rabbit" },
                    { 4, "Bird" },
                    { 5, "Guinea Pig" },
                    { 6, "Hamster" },
                    { 7, "Ferret" },
                    { 8, "Other" }
                });

            migrationBuilder.InsertData(
                schema: "shelter_hub",
                table: "pet_breeds",
                columns: new[] { "BreedId", "Name", "SpeciesId" },
                values: new object[,]
                {
                    { 1, "Mixed Breed", 1 },
                    { 2, "Labrador Retriever", 1 },
                    { 3, "Golden Retriever", 1 },
                    { 4, "German Shepherd", 1 },
                    { 5, "French Bulldog", 1 },
                    { 6, "Bulldog", 1 },
                    { 7, "Poodle", 1 },
                    { 8, "Beagle", 1 },
                    { 9, "Rottweiler", 1 },
                    { 10, "Yorkshire Terrier", 1 },
                    { 11, "Chihuahua", 1 },
                    { 12, "Border Collie", 1 },
                    { 13, "Australian Shepherd", 1 },
                    { 14, "Siberian Husky", 1 },
                    { 15, "Boxer", 1 },
                    { 16, "Domestic Shorthair", 2 },
                    { 17, "Domestic Longhair", 2 },
                    { 18, "Persian", 2 },
                    { 19, "Maine Coon", 2 },
                    { 20, "Ragdoll", 2 },
                    { 21, "British Shorthair", 2 },
                    { 22, "Siamese", 2 },
                    { 23, "American Shorthair", 2 },
                    { 24, "Russian Blue", 2 },
                    { 25, "Bengal", 2 },
                    { 26, "Mixed Breed", 3 },
                    { 27, "Holland Lop", 3 },
                    { 28, "Netherland Dwarf", 3 },
                    { 29, "Mini Rex", 3 },
                    { 30, "Lionhead", 3 },
                    { 31, "Budgerigar", 4 },
                    { 32, "Cockatiel", 4 },
                    { 33, "Canary", 4 },
                    { 34, "Lovebird", 4 },
                    { 35, "Conure", 4 },
                    { 36, "American Guinea Pig", 5 },
                    { 37, "Peruvian Guinea Pig", 5 },
                    { 38, "Abyssinian Guinea Pig", 5 },
                    { 39, "Syrian Hamster", 6 },
                    { 40, "Dwarf Hamster", 6 },
                    { 41, "Domestic Ferret", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_pet_breeds_SpeciesId",
                schema: "shelter_hub",
                table: "pet_breeds",
                column: "SpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_BreedId",
                schema: "shelter_hub",
                table: "pets",
                column: "BreedId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_ShelterId",
                schema: "shelter_hub",
                table: "pets",
                column: "ShelterId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_SpeciesId",
                schema: "shelter_hub",
                table: "pets",
                column: "SpeciesId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_Status",
                schema: "shelter_hub",
                table: "pets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_pets_Status_SpeciesId_BreedId",
                schema: "shelter_hub",
                table: "pets",
                columns: new[] { "Status", "SpeciesId", "BreedId" });

            migrationBuilder.CreateIndex(
                name: "IX_shelters_Location",
                schema: "shelter_hub",
                table: "shelters",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pets",
                schema: "shelter_hub");

            migrationBuilder.DropTable(
                name: "pet_breeds",
                schema: "shelter_hub");

            migrationBuilder.DropTable(
                name: "shelters",
                schema: "shelter_hub");

            migrationBuilder.DropTable(
                name: "pet_species",
                schema: "shelter_hub");
        }
    }
}
