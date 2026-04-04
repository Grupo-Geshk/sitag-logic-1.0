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
            migrationBuilder.CreateTable(
                name: "farm_brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_farm_brands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_farm_brands_farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_farm_brands_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_farm_brands_FarmId",
                table: "farm_brands",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_farm_brands_TenantId",
                table: "farm_brands",
                column: "TenantId");

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "animals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BrandedAt",
                table: "animals",
                type: "timestamptz",
                nullable: true);

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

            migrationBuilder.DropIndex(
                name: "IX_animals_BrandId",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "BrandedAt",
                table: "animals");

            migrationBuilder.DropTable(
                name: "farm_brands");
        }
    }
}
