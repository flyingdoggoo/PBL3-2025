using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PBL3.Migrations
{
    /// <inheritdoc />
    public partial class AddAirportsAndUpdateFlights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StartingDestination",
                table: "Flights",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "ReachingDestination",
                table: "Flights",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "Flights",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Airports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_ReachingDestination",
                table: "Flights",
                column: "ReachingDestination");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_StartingDestination",
                table: "Flights",
                column: "StartingDestination");

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_Airports_ReachingDestination",
                table: "Flights",
                column: "ReachingDestination",
                principalTable: "Airports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_Airports_StartingDestination",
                table: "Flights",
                column: "StartingDestination",
                principalTable: "Airports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flights_Airports_ReachingDestination",
                table: "Flights");

            migrationBuilder.DropForeignKey(
                name: "FK_Flights_Airports_StartingDestination",
                table: "Flights");

            migrationBuilder.DropTable(
                name: "Airports");

            migrationBuilder.DropIndex(
                name: "IX_Flights_ReachingDestination",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_StartingDestination",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "Flights");

            migrationBuilder.AlterColumn<string>(
                name: "StartingDestination",
                table: "Flights",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ReachingDestination",
                table: "Flights",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
