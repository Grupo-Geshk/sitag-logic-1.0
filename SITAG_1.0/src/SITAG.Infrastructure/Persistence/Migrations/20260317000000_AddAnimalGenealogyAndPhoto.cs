using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalGenealogyAndPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Add new genealogy columns ──────────────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "MotherId",
                table: "animals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherRef",
                table: "animals",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FatherId",
                table: "animals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherRef",
                table: "animals",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // ── 2. Add photo column ───────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "animals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // ── 3. Migrate existing data: ParentId was always the mother ─────
            // SEMANTIC NOTE: All existing ParentId values represent mothers.
            // Any ParentId pointing to a male animal is treated as mother reference
            // (data integrity issue that predated this migration — should be reviewed
            // post-migration with the query:
            //   SELECT a.* FROM animals a JOIN animals p ON a."MotherId" = p."Id"
            //   WHERE p."Sex" = 'Macho';
            // )
            migrationBuilder.Sql(@"UPDATE animals SET ""MotherId"" = ""ParentId"" WHERE ""ParentId"" IS NOT NULL;");

            // ── 4. Add FK constraints for new genealogy columns ───────────────
            migrationBuilder.AddForeignKey(
                name: "FK_animals_animals_MotherId",
                table: "animals",
                column: "MotherId",
                principalTable: "animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_animals_animals_FatherId",
                table: "animals",
                column: "FatherId",
                principalTable: "animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── 5. Add indexes for offspring queries ──────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_animals_MotherId",
                table: "animals",
                column: "MotherId");

            migrationBuilder.CreateIndex(
                name: "IX_animals_FatherId",
                table: "animals",
                column: "FatherId");

            // ── 6. Drop the old ParentId FK, index, and column ────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_animals_animals_ParentId",
                table: "animals");

            migrationBuilder.DropIndex(
                name: "IX_animals_ParentId",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "animals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Restore ParentId column ───────────────────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "animals",
                type: "uuid",
                nullable: true);

            // Restore ParentId from MotherId
            migrationBuilder.Sql(@"UPDATE animals SET ""ParentId"" = ""MotherId"" WHERE ""MotherId"" IS NOT NULL;");

            migrationBuilder.AddForeignKey(
                name: "FK_animals_animals_ParentId",
                table: "animals",
                column: "ParentId",
                principalTable: "animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateIndex(
                name: "IX_animals_ParentId",
                table: "animals",
                column: "ParentId");

            // ── Drop new columns ───────────────────────────────────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_animals_animals_MotherId",
                table: "animals");

            migrationBuilder.DropForeignKey(
                name: "FK_animals_animals_FatherId",
                table: "animals");

            migrationBuilder.DropIndex(
                name: "IX_animals_MotherId",
                table: "animals");

            migrationBuilder.DropIndex(
                name: "IX_animals_FatherId",
                table: "animals");

            migrationBuilder.DropColumn(name: "MotherId",  table: "animals");
            migrationBuilder.DropColumn(name: "MotherRef", table: "animals");
            migrationBuilder.DropColumn(name: "FatherId",  table: "animals");
            migrationBuilder.DropColumn(name: "FatherRef", table: "animals");
            migrationBuilder.DropColumn(name: "PhotoUrl",  table: "animals");
        }
    }
}
