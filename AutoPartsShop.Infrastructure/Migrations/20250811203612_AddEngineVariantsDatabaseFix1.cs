using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineVariantsDatabaseFix1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompatibleYears",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "EngineSize",
                table: "CarModels");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "CarModels");

            migrationBuilder.CreateTable(
                name: "EngineVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarModelId = table.Column<int>(type: "int", nullable: false),
                    FuelType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EngineSize = table.Column<int>(type: "int", nullable: false),
                    YearFrom = table.Column<int>(type: "int", nullable: false),
                    YearTo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EngineVariants_CarModels_CarModelId",
                        column: x => x.CarModelId,
                        principalTable: "CarModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartEngineVariants",
                columns: table => new
                {
                    PartId = table.Column<int>(type: "int", nullable: false),
                    EngineVariantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartEngineVariants", x => new { x.PartId, x.EngineVariantId });
                    table.ForeignKey(
                        name: "FK_PartEngineVariants_EngineVariants_EngineVariantId",
                        column: x => x.EngineVariantId,
                        principalTable: "EngineVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartEngineVariants_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EngineVariants_CarModelId",
                table: "EngineVariants",
                column: "CarModelId");

            migrationBuilder.CreateIndex(
                name: "IX_PartEngineVariants_EngineVariantId",
                table: "PartEngineVariants",
                column: "EngineVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartEngineVariants");

            migrationBuilder.DropTable(
                name: "EngineVariants");

            migrationBuilder.AddColumn<string>(
                name: "CompatibleYears",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EngineSize",
                table: "CarModels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "CarModels",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
