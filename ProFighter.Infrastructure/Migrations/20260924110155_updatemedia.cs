using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProFighter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatemedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Gyms_OwnerId",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Products_OwnerId",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Trainers_OwnerId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_OwnerId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_OwnerType_OwnerId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_OwnerType_OwnerId_Purpose",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "Medias");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Medias",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "Medias",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Medias",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "TrainerId",
                table: "Medias",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_CustomerId",
                table: "Medias",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_CustomerId_Purpose_DisplayOrder",
                table: "Medias",
                columns: new[] { "CustomerId", "Purpose", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Medias_GymId",
                table: "Medias",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_GymId_Purpose_DisplayOrder",
                table: "Medias",
                columns: new[] { "GymId", "Purpose", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Medias_ProductId",
                table: "Medias",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_ProductId_Purpose_DisplayOrder",
                table: "Medias",
                columns: new[] { "ProductId", "Purpose", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Medias_TrainerId",
                table: "Medias",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_TrainerId_Purpose_DisplayOrder",
                table: "Medias",
                columns: new[] { "TrainerId", "Purpose", "DisplayOrder" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Media_SingleOwner",
                table: "Medias",
                sql: "((CASE WHEN `CustomerId` IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN `TrainerId`  IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN `GymId`     IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN `ProductId` IS NOT NULL THEN 1 ELSE 0 END)) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Customers_CustomerId",
                table: "Medias",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Gyms_GymId",
                table: "Medias",
                column: "GymId",
                principalTable: "Gyms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Products_ProductId",
                table: "Medias",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Trainers_TrainerId",
                table: "Medias",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Customers_CustomerId",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Gyms_GymId",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Products_ProductId",
                table: "Medias");

            migrationBuilder.DropForeignKey(
                name: "FK_Medias_Trainers_TrainerId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_CustomerId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_CustomerId_Purpose_DisplayOrder",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_GymId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_GymId_Purpose_DisplayOrder",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_ProductId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_ProductId_Purpose_DisplayOrder",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_TrainerId",
                table: "Medias");

            migrationBuilder.DropIndex(
                name: "IX_Medias_TrainerId_Purpose_DisplayOrder",
                table: "Medias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Media_SingleOwner",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Medias");

            migrationBuilder.DropColumn(
                name: "TrainerId",
                table: "Medias");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Medias",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "OwnerType",
                table: "Medias",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_OwnerId",
                table: "Medias",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_OwnerType_OwnerId",
                table: "Medias",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Medias_OwnerType_OwnerId_Purpose",
                table: "Medias",
                columns: new[] { "OwnerType", "OwnerId", "Purpose" });

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Gyms_OwnerId",
                table: "Medias",
                column: "OwnerId",
                principalTable: "Gyms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Products_OwnerId",
                table: "Medias",
                column: "OwnerId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Medias_Trainers_OwnerId",
                table: "Medias",
                column: "OwnerId",
                principalTable: "Trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
