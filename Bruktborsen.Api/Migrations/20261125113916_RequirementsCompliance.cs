using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bruktborsen.Api.Migrations
{
    public partial class RequirementsCompliance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_ListingId",
                table: "Offers");

            migrationBuilder.RenameColumn(
                name: "ChangedAt",
                table: "PriceChanges",
                newName: "ChangedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Offers",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "SoldAt",
                table: "Listings",
                newName: "SoldDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Listings",
                newName: "CreatedDate");

            migrationBuilder.AlterColumn<int>(
                name: "Price",
                table: "Listings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Listings",
                type: "TEXT",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Listings",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldPrice",
                table: "Listings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_UniquePending",
                table: "Offers",
                columns: new[] { "ListingId", "BuyerId" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Price_Positive",
                table: "Listings",
                sql: "\"Price\" > 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_UniquePending",
                table: "Offers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Price_Positive",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SoldPrice",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "ChangedDate",
                table: "PriceChanges",
                newName: "ChangedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Offers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Listings",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "SoldDate",
                table: "Listings",
                newName: "SoldAt");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Listings",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ListingId",
                table: "Offers",
                column: "ListingId");
        }
    }
}