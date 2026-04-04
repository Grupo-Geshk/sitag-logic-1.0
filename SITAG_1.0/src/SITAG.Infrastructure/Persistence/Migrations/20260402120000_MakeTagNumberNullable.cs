using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class MakeTagNumberNullable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the old unique index (covers all rows including empty strings)
        migrationBuilder.DropIndex(
            name: "ix_animals_tenant_id_tag_number",
            table: "animals");

        // Allow NULL on tag_number
        migrationBuilder.AlterColumn<string>(
            name: "tag_number",
            table: "animals",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        // Recreate as partial unique index — only enforce uniqueness when a tag is set
        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX ix_animals_tenant_id_tag_number " +
            "ON animals(tenant_id, tag_number) " +
            "WHERE tag_number IS NOT NULL;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_animals_tenant_id_tag_number;");

        // Restore empty string for any NULLs before making column non-nullable again
        migrationBuilder.Sql("UPDATE animals SET tag_number = '' WHERE tag_number IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "tag_number",
            table: "animals",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_animals_tenant_id_tag_number",
            table: "animals",
            columns: new[] { "tenant_id", "tag_number" },
            unique: true);
    }
}
