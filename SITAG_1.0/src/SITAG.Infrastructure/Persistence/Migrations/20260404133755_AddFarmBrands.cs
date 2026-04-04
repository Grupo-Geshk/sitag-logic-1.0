using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmBrands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "animals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BrandedAt",
                table: "animals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "farm_brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_farm_brands", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_animals_BrandId",
                table: "animals",
                column: "BrandId");

            migrationBuilder.AddForeignKey(
                name: "FK_animals_farm_brands_BrandId",
                table: "animals",
                column: "BrandId",
                principalTable: "farm_brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_animals_farm_brands_BrandId",
                table: "animals");

            migrationBuilder.DropTable(
                name: "farm_brands");

            migrationBuilder.DropIndex(
                name: "IX_animals_BrandId",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "BrandedAt",
                table: "animals");
        }
    }
}
