using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empire.Web.Migrations
{
    /// <inheritdoc />
    public partial class initialdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Users_UserId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_ShopId_CreatedDate",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_ShopId_Status",
                table: "Repairs");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "StockMovements",
                newName: "CreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_UserId",
                table: "StockMovements",
                newName: "IX_StockMovements_CreatedBy");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "StockMovements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCost",
                table: "StockMovements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MovementDate",
                table: "StockMovements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "StockMovements",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "Repairs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BrandId",
                table: "Repairs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceCategoryId",
                table: "Repairs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceModelId",
                table: "Repairs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "LookupValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "InventoryItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "InventoryItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_BrandId",
                table: "Repairs",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_DeviceCategoryId",
                table: "Repairs",
                column: "DeviceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_DeviceModelId",
                table: "Repairs",
                column: "DeviceModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_ShopId",
                table: "Repairs",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_CategoryId",
                table: "LookupValues",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryType_DisplayOrder",
                table: "Categories",
                columns: new[] { "CategoryType", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Categories_CategoryId",
                table: "InventoryItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LookupValues_Categories_CategoryId",
                table: "LookupValues",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Brands_BrandId",
                table: "Repairs",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_DeviceCategories_DeviceCategoryId",
                table: "Repairs",
                column: "DeviceCategoryId",
                principalTable: "DeviceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_DeviceModels_DeviceModelId",
                table: "Repairs",
                column: "DeviceModelId",
                principalTable: "DeviceModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Users_CreatedBy",
                table: "StockMovements",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Categories_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LookupValues_Categories_CategoryId",
                table: "LookupValues");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Brands_BrandId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_DeviceCategories_DeviceCategoryId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_DeviceModels_DeviceModelId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Users_CreatedBy",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_BrandId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_DeviceCategoryId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_DeviceModelId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_ShopId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_LookupValues_CategoryId",
                table: "LookupValues");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "MovementDate",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "DeviceCategoryId",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "DeviceModelId",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "LookupValues");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "StockMovements",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_CreatedBy",
                table: "StockMovements",
                newName: "IX_StockMovements_UserId");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "StockMovements",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCost",
                table: "StockMovements",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "Repairs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_ShopId_CreatedDate",
                table: "Repairs",
                columns: new[] { "ShopId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_ShopId_Status",
                table: "Repairs",
                columns: new[] { "ShopId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Users_UserId",
                table: "StockMovements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
