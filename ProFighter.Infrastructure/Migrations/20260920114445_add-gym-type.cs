using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFighter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addgymtype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Trainers");

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "RekazWebhookInboxEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GymType",
                table: "RekazWebhookInboxEntries");

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Trainers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
