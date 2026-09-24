using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFighter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatetrainers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrainingType",
                table: "Trainers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainingType",
                table: "Trainers");
        }
    }
}
