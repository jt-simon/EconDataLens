using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EconDataLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeCpiDataValueNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                table: "cpi_data",
                type: "numeric(18,3)",
                maxLength: 12,
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldMaxLength: 12,
                oldPrecision: 18,
                oldScale: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                table: "cpi_data",
                type: "numeric(18,3)",
                maxLength: 12,
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldMaxLength: 12,
                oldPrecision: 18,
                oldScale: 3,
                oldNullable: true);
        }
    }
}
