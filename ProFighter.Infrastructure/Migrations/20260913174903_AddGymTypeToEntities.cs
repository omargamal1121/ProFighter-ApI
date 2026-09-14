using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFighter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGymTypeToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_MobileNumber",
                table: "Customers");

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "Trainers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "NotificationLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "MerchandiseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "LoyaltyTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "Gifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "DeviceTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "CustomerSyncFailures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GymType",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email_GymType",
                table: "Customers",
                columns: new[] { "Email", "GymType" },
                unique: true,
                filter: "`Email` IS NOT NULL AND `DeletedAt` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MobileNumber_GymType",
                table: "Customers",
                columns: new[] { "MobileNumber", "GymType" },
                unique: true,
                filter: "`DeletedAt` IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Email_GymType",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_MobileNumber_GymType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "MerchandiseOrders");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "Gifts");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "CustomerSyncFailures");

            migrationBuilder.DropColumn(
                name: "GymType",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true,
                filter: "`Email` IS NOT NULL AND `DeletedAt` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MobileNumber",
                table: "Customers",
                column: "MobileNumber",
                unique: true,
                filter: "`DeletedAt` IS NULL");
        }
    }
}
