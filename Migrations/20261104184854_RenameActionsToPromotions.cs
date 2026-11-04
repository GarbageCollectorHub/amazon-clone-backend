using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMZN.Migrations
{
    public partial class RenameActionsToPromotions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "Actions", schema: "AMZN", newName: "Promotions", newSchema: "AMZN");
            migrationBuilder.RenameTable(name: "ProductActions", schema: "AMZN", newName: "ProductPromotions", newSchema: "AMZN");
            migrationBuilder.RenameTable(name: "CategoryActions", schema: "AMZN", newName: "CategoryPromotions", newSchema: "AMZN");

            migrationBuilder.RenameColumn(name: "Amount", schema: "AMZN", table: "Promotions", newName: "DiscountPercent");
            migrationBuilder.RenameColumn(name: "ActionId", schema: "AMZN", table: "ProductPromotions", newName: "PromotionId");
            migrationBuilder.RenameColumn(name: "ActionId", schema: "AMZN", table: "CategoryPromotions", newName: "PromotionId");

            migrationBuilder.RenameIndex(name: "IX_ProductActions_ActionId", schema: "AMZN", table: "ProductPromotions", newName: "IX_ProductPromotions_PromotionId");
            migrationBuilder.RenameIndex(name: "IX_ProductActions_ProductId", schema: "AMZN", table: "ProductPromotions", newName: "IX_ProductPromotions_ProductId");
            migrationBuilder.RenameIndex(name: "IX_CategoryActions_ActionId", schema: "AMZN", table: "CategoryPromotions", newName: "IX_CategoryPromotions_PromotionId");
            migrationBuilder.RenameIndex(name: "IX_CategoryActions_CategoryId", schema: "AMZN", table: "CategoryPromotions", newName: "IX_CategoryPromotions_CategoryId");

            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[PK_Actions]', N'PK_Promotions', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[PK_ProductActions]', N'PK_ProductPromotions', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[PK_CategoryActions]', N'PK_CategoryPromotions', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_ProductActions_Actions_ActionId]', N'FK_ProductPromotions_Promotions_PromotionId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_ProductActions_Products_ProductId]', N'FK_ProductPromotions_Products_ProductId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_CategoryActions_Actions_ActionId]', N'FK_CategoryPromotions_Promotions_PromotionId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_CategoryActions_Categories_CategoryId]', N'FK_CategoryPromotions_Categories_CategoryId', N'OBJECT';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_CategoryPromotions_Categories_CategoryId]', N'FK_CategoryActions_Categories_CategoryId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_CategoryPromotions_Promotions_PromotionId]', N'FK_CategoryActions_Actions_ActionId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_ProductPromotions_Products_ProductId]', N'FK_ProductActions_Products_ProductId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[FK_ProductPromotions_Promotions_PromotionId]', N'FK_ProductActions_Actions_ActionId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[PK_CategoryPromotions]', N'PK_CategoryActions', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[PK_ProductPromotions]', N'PK_ProductActions', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[AMZN].[PK_Promotions]', N'PK_Actions', N'OBJECT';");

            migrationBuilder.RenameIndex(name: "IX_CategoryPromotions_CategoryId", schema: "AMZN", table: "CategoryPromotions", newName: "IX_CategoryActions_CategoryId");
            migrationBuilder.RenameIndex(name: "IX_CategoryPromotions_PromotionId", schema: "AMZN", table: "CategoryPromotions", newName: "IX_CategoryActions_ActionId");
            migrationBuilder.RenameIndex(name: "IX_ProductPromotions_ProductId", schema: "AMZN", table: "ProductPromotions", newName: "IX_ProductActions_ProductId");
            migrationBuilder.RenameIndex(name: "IX_ProductPromotions_PromotionId", schema: "AMZN", table: "ProductPromotions", newName: "IX_ProductActions_ActionId");

            migrationBuilder.RenameColumn(name: "PromotionId", schema: "AMZN", table: "CategoryPromotions", newName: "ActionId");
            migrationBuilder.RenameColumn(name: "PromotionId", schema: "AMZN", table: "ProductPromotions", newName: "ActionId");
            migrationBuilder.RenameColumn(name: "DiscountPercent", schema: "AMZN", table: "Promotions", newName: "Amount");

            migrationBuilder.RenameTable(name: "CategoryPromotions", schema: "AMZN", newName: "CategoryActions", newSchema: "AMZN");
            migrationBuilder.RenameTable(name: "ProductPromotions", schema: "AMZN", newName: "ProductActions", newSchema: "AMZN");
            migrationBuilder.RenameTable(name: "Promotions", schema: "AMZN", newName: "Actions", newSchema: "AMZN");
        }
    }
}
