using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable MA0051
#pragma warning disable CA1861

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnlineArticleMappings",
                columns: table => new
                {
                    OnlineArticleMappingId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticleName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StoreKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StoreProductCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StoreProductName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StoreProductPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    MatchMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    QuantityPerUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<string>(type: "TEXT", nullable: false),
                    FeedbackCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineArticleMappings", x => x.OnlineArticleMappingId);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingPreferences",
                columns: table => new
                {
                    ShoppingPreferenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StoreKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingPreferences", x => x.ShoppingPreferenceId);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingSessions",
                columns: table => new
                {
                    ShoppingSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IngredientList = table.Column<string>(type: "TEXT", maxLength: 50000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingSessions", x => x.ShoppingSessionId);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingSessionItems",
                columns: table => new
                {
                    ShoppingSessionItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShoppingSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalIngredient = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SelectedProductName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SelectedProductUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AddedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingSessionItems", x => x.ShoppingSessionItemId);
                    table.ForeignKey(
                        name: "FK_ShoppingSessionItems_ShoppingSessions_ShoppingSessionId",
                        column: x => x.ShoppingSessionId,
                        principalTable: "ShoppingSessions",
                        principalColumn: "ShoppingSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineArticleMappings_ArticleName_StoreKey_StoreProductCode",
                table: "OnlineArticleMappings",
                columns: new[] { "ArticleName", "StoreKey", "StoreProductCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineArticleMappings_StoreKey_ArticleName",
                table: "OnlineArticleMappings",
                columns: new[] { "StoreKey", "ArticleName" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingPreferences_Scope_Key_StoreKey",
                table: "ShoppingPreferences",
                columns: new[] { "Scope", "Key", "StoreKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingSessionItems_ShoppingSessionId",
                table: "ShoppingSessionItems",
                column: "ShoppingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingSessions_StartedAt",
                table: "ShoppingSessions",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnlineArticleMappings");

            migrationBuilder.DropTable(
                name: "ShoppingPreferences");

            migrationBuilder.DropTable(
                name: "ShoppingSessionItems");

            migrationBuilder.DropTable(
                name: "ShoppingSessions");
        }
    }
}
