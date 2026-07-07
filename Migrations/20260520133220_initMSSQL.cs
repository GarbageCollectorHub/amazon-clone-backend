using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMZN.Migrations
{
    /// <inheritdoc />
    public partial class initMSSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AMZN");

            migrationBuilder.CreateTable(
                name: "Actions",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "AMZN",
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeletedUsers",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryActions",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalSchema: "AMZN",
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryActions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "AMZN",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carts_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAddresses",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryAddresses_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrimaryImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    RatingSum = table.Column<int>(type: "int", nullable: false),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Product_CurrentPrice", "CurrentPrice >= 0");
                    table.CheckConstraint("CK_Product_OriginalPrice", "OriginalPrice IS NULL OR OriginalPrice >= CurrentPrice");
                    table.CheckConstraint("CK_Product_RatingCount", "RatingCount >= 0");
                    table.CheckConstraint("CK_Product_RatingSum", "RatingSum >= 0");
                    table.CheckConstraint("CK_Product_StockQuantity", "StockQuantity >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "AMZN",
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "AMZN",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Users_SellerId",
                        column: x => x.SellerId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_DeliveryAddresses_DeliveryAddressId",
                        column: x => x.DeliveryAddressId,
                        principalSchema: "AMZN",
                        principalTable: "DeliveryAddresses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalSchema: "AMZN",
                        principalTable: "PaymentMethods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalSchema: "AMZN",
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "AMZN",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductActions",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalSchema: "AMZN",
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductActions_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "AMZN",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.CheckConstraint("CK_ProductImage_SortOrder", "SortOrder >= 0");
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "AMZN",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductQuestions",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "AMZN",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductRatings",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRatings", x => x.Id);
                    table.CheckConstraint("CK_ProductRating_Value", "Value >= 1 AND Value <= 5");
                    table.ForeignKey(
                        name: "FK_ProductRatings_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "AMZN",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductRatings_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductReviews",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "AMZN",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductReviews_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "AMZN",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductQuestionAnswers",
                schema: "AMZN",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuestionAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductQuestionAnswers_ProductQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "AMZN",
                        principalTable: "ProductQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductQuestionAnswers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AMZN",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                schema: "AMZN",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                schema: "AMZN",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                schema: "AMZN",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                schema: "AMZN",
                table: "Carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "AMZN",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                schema: "AMZN",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryActions_ActionId",
                schema: "AMZN",
                table: "CategoryActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryActions_CategoryId",
                schema: "AMZN",
                table: "CategoryActions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAddresses_UserId",
                schema: "AMZN",
                table: "DeliveryAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "AMZN",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryAddressId",
                schema: "AMZN",
                table: "Orders",
                column: "DeliveryAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentMethodId",
                schema: "AMZN",
                table: "Orders",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                schema: "AMZN",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId",
                schema: "AMZN",
                table: "PaymentMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductActions_ActionId",
                schema: "AMZN",
                table: "ProductActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductActions_ProductId",
                schema: "AMZN",
                table: "ProductActions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                schema: "AMZN",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_SortOrder",
                schema: "AMZN",
                table: "ProductImages",
                columns: new[] { "ProductId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestionAnswers_QuestionId_CreatedAt",
                schema: "AMZN",
                table: "ProductQuestionAnswers",
                columns: new[] { "QuestionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestionAnswers_UserId",
                schema: "AMZN",
                table: "ProductQuestionAnswers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_ProductId_CreatedAt",
                schema: "AMZN",
                table: "ProductQuestions",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_UserId",
                schema: "AMZN",
                table: "ProductQuestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRatings_ProductId_UserId",
                schema: "AMZN",
                table: "ProductRatings",
                columns: new[] { "ProductId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductRatings_UserId",
                schema: "AMZN",
                table: "ProductRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId_CreatedAt",
                schema: "AMZN",
                table: "ProductReviews",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId_UserId",
                schema: "AMZN",
                table: "ProductReviews",
                columns: new[] { "ProductId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId",
                schema: "AMZN",
                table: "ProductReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                schema: "AMZN",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_CreatedAt",
                schema: "AMZN",
                table: "Products",
                columns: new[] { "CategoryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SellerId",
                schema: "AMZN",
                table: "Products",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Title",
                schema: "AMZN",
                table: "Products",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_TokenHash",
                schema: "AMZN",
                table: "UserRefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId",
                schema: "AMZN",
                table: "UserRefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "AMZN",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "CategoryActions",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "DeletedUsers",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "ProductActions",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "ProductImages",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "ProductQuestionAnswers",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "ProductRatings",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "ProductReviews",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "Carts",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "Actions",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "ProductQuestions",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "DeliveryAddresses",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "Brands",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "AMZN");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "AMZN");
        }
    }
}
