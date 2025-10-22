using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empire.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Shops_ShopId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Shops_ShopId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Shops_ShopId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Inventories_InventoryId",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Customers_CustomerId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Shops_ShopId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserShopRoles_Shops_ShopId",
                table: "UserShopRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserShopRoles_Users_UserId",
                table: "UserShopRoles");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Shops_ShopId",
                table: "Customers",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Shops_ShopId",
                table: "Devices",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Shops_ShopId",
                table: "Inventories",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Inventories_InventoryId",
                table: "InventoryAdjustments",
                column: "InventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Customers_CustomerId",
                table: "Repairs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Shops_ShopId",
                table: "Repairs",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserShopRoles_Shops_ShopId",
                table: "UserShopRoles",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserShopRoles_Users_UserId",
                table: "UserShopRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Shops_ShopId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Shops_ShopId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Shops_ShopId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Inventories_InventoryId",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Customers_CustomerId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Shops_ShopId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserShopRoles_Shops_ShopId",
                table: "UserShopRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserShopRoles_Users_UserId",
                table: "UserShopRoles");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Shops_ShopId",
                table: "Customers",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Shops_ShopId",
                table: "Devices",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Shops_ShopId",
                table: "Inventories",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Inventories_InventoryId",
                table: "InventoryAdjustments",
                column: "InventoryId",
                principalTable: "Inventories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Customers_CustomerId",
                table: "Repairs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Devices_DeviceId",
                table: "Repairs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Shops_ShopId",
                table: "Repairs",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserShopRoles_Shops_ShopId",
                table: "UserShopRoles",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserShopRoles_Users_UserId",
                table: "UserShopRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
