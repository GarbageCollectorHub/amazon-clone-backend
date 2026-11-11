using System.Data.SqlTypes;
using AMZN.Data;
using AMZN.Data.Entities;
using AMZN.Models.Promotions;
using AMZN.Repositories.Carts;
using AMZN.Repositories.Categories;
using AMZN.Repositories.Orders;
using AMZN.Repositories.Products;
using AMZN.Repositories.Products.Queries;
using AMZN.Repositories.Promotions;
using AMZN.Repositories.Users;
using AMZN.Services.Admin;
using AMZN.Services.Carts;
using AMZN.Services.Orders;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Transactions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AMZN.Tests;

public class SqlQueryWorkflowTests
{
    // У всех товаров совпадают цена, дата и рейтинг. SQL должен использовать Id как последний ключ,
    // чтобы при неизменном каталоге соседние страницы не пересекались и возвращали все товары.
    [LocalDbFact]
    public Task CatalogPages_UseStableOrderWhenSortValuesMatch()
    {
        return WithDatabaseAsync(async (db, seller, products) =>
        {
            var repository = new ProductRepository(db);
            var expected = products.OrderBy(p =>
            {
                return new SqlGuid(p.Id);
            }).Select(GetProductId).ToArray();
            foreach (string sort in new[] { "price_asc", "price_desc", "newest", "featured", "rating" })
            {
                var query = new ProductListQueryParams { Sort = sort };
                var first = await repository.GetCatalogProductsAsync(query, 0, 20, null);
                var second = await repository.GetCatalogProductsAsync(query, 20, 20, null);
                var repeated = await repository.GetCatalogProductsAsync(query, 0, 20, null);
                Assert.Equal(first.Select(GetProductId), repeated.Select(GetProductId));
                Assert.Equal(expected, first.Concat(second).Select(GetProductId));
            }

            var sellerPage = await repository.GetSellerProductsAsync(seller.Id, "Tablet", "price-desc", 2, 20, default);
            Assert.Equal(products.Count, sellerPage.TotalItems);
            Assert.Equal(2, sellerPage.Page);
            Assert.Equal(products.Count - 20, sellerPage.Items.Count);
            Assert.Empty((await repository.GetSellerProductsAsync(seller.Id, "missing", "newest", 1, 20, default)).Items);
        });

        static Guid GetProductId(Product product)
        {
            return product.Id;
        }
    }

    // Корзина становится черновиком заказа и затем обновляется. Если одного товара не хватает,
    // подтверждение не должно менять ни остатки, ни корзину. После пополнения тот же заказ подтверждается.
    [LocalDbFact]
    public Task Checkout_WhenStockIsInsufficient_KeepsOrderAndCartIntact()
    {
        return WithDatabaseAsync(async (db, seller, products) =>
        {
            var address = new DeliveryAddress
            {
                UserId = seller.Id, FirstName = "Demo", LastName = "Seller", Address = "Test Street",
                City = "Test City", Country = "Test Country", PostalCode = "00000", PhoneNumber = "0000000000"
            };
            var payment = new PaymentMethod
            {
                UserId = seller.Id, CardNumber = "4111111111111111", ExpirationDate = new DateOnly(2099, 1, 1)
            };
            db.AddRange(address, payment);
            await db.SaveChangesAsync();
            await db.Products.Where(p => p.Id == products[1].Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, 0));
            db.ChangeTracker.Clear();

            var cartRepository = new CartRepository(db);
            var productRepository = new ProductRepository(db);
            var userRepository = new UserRepository(db);
            var cart = new CartService(cartRepository, productRepository, userRepository);
            var order = new OrderService(new OrderRepository(db), cartRepository, productRepository, userRepository,
                new EfTransactionManager(db));

            var firstCart = await cart.AddToCartAsync(seller.Id, products[0].Id);
            Assert.NotNull(firstCart.Items);
            Assert.Single(firstCart.Items);
            Assert.Equal(2, (await cart.AddToCartAsync(seller.Id, products[1].Id)).Items?.Count);
            Assert.Equal(2, (await order.CreateOrderAsync(seller.Id)).TotalQuantity);
            await cart.IncreaseQuantityAsync(seller.Id, products[0].Id);
            Assert.Equal(3, (await order.UpdateOrderAsync(seller.Id)).TotalQuantity);
            db.ChangeTracker.Clear();

            var rejected = await Assert.ThrowsAsync<ApiException>(() =>
            {
                return order.CompleteOrderAsync(seller.Id, payment.Id, address.Id);
            });
            Assert.Equal(409, rejected.StatusCode);
            db.ChangeTracker.Clear();
            Assert.Equal(5, await db.Products.Where(p => p.Id == products[0].Id).Select(p => p.StockQuantity).SingleAsync());
            Assert.Equal(2, await db.CartItems.CountAsync());
            Assert.Equal(OrderStatus.Pending, await db.Orders.Select(o => o.Status).SingleAsync());

            await db.Products.Where(p => p.Id == products[1].Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, 1));
            Assert.Equal(OrderStatus.Confirmed, (await order.CompleteOrderAsync(seller.Id, payment.Id, address.Id)).Status);
            Assert.Equal(0, await db.CartItems.CountAsync());
            Assert.Equal(3, await db.Products.Where(p => p.Id == products[0].Id).Select(p => p.StockQuantity).SingleAsync());
        });
    }

    // У товара одновременно действуют скидка категории и собственная скидка. Когда скидка категории
    // заканчивается, остальные товары возвращаются к исходной цене, а активная скидка товара сохраняется.
    [LocalDbFact]
    public Task Promotions_ExpiredCategoryDiscountDoesNotRemoveActiveProductDiscount()
    {
        return WithDatabaseAsync(async (db, _, products) =>
        {
            var promotions = new AdminPromotionService(new PromotionRepository(db), new ProductRepository(db),
                new CategoryRepository(db), new EfTransactionManager(db));
            DateTime now = DateTime.UtcNow;
            Assert.Empty(await promotions.AddPromotionAsync(new PromotionFormModel
            {
                Name = "Category offer", ApplyTo = "category", ProductTitle = "Tablets",
                DiscountPercent = 10, StartDate = now.AddDays(-2), EndDate = now.AddDays(1)
            }));
            Assert.Empty(await promotions.AddPromotionAsync(new PromotionFormModel
            {
                Name = "Product offer", ApplyTo = "product", ProductTitle = products[0].Title,
                DiscountPercent = 20, StartDate = now.AddDays(-1), EndDate = now.AddDays(2)
            }));
            Assert.Equal(80m, await PriceAsync(products[0].Id));
            Assert.Equal(90m, await PriceAsync(products[1].Id));

            await db.Promotions.Where(p => p.Name == "Category offer")
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.EndDate, now.AddDays(-1)));
            db.ChangeTracker.Clear();
            await promotions.CheckPromotionsValidityAsync();
            Assert.Equal(80m, await PriceAsync(products[0].Id));
            Assert.Equal(100m, await PriceAsync(products[1].Id));

            Task<decimal> PriceAsync(Guid id)
            {
                return db.Products.Where(p => p.Id == id).Select(p => p.CurrentPrice).SingleAsync();
            }
        });
    }

    private static async Task WithDatabaseAsync(Func<DataContext, User, List<Product>, Task> test)
    {
        // Каждый тест создаёт и удаляет свою базу, не используя строку подключения приложения.
        string connection = $@"Server=(localdb)\MSSQLLocalDB;Database=AmznQueryTests_{Guid.NewGuid():N};Integrated Security=True;TrustServerCertificate=True;";
        await using var db = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlServer(connection).Options);
        try
        {
            await db.Database.MigrateAsync();
            var seller = new User { FirstName = "Demo", LastName = "Seller", Email = "seller@example.test", PasswordHash = "unused" };
            var category = new Category { Name = "Tablets", Description = "", ImageUrl = "/tablet.png" };
            var brand = new Brand { Name = "Demo" };
            var created = new DateTime(2026, 1, 1);
            var products = Enumerable.Range(1, 23).Select(number =>
            {
                return new Product
                {
                    Seller = seller, Category = category, Brand = brand, Title = $"Tablet {number:00}",
                    CurrentPrice = 100m, CreatedAt = created, PrimaryImageUrl = "/tablet.png", StockQuantity = 5
                };
            }).ToList();
            db.AddRange(products);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await test(db, seller, products);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private sealed class LocalDbFactAttribute : FactAttribute
    {
        public LocalDbFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("AMZN_TEST_LOCALDB") != "1")
                Skip = "Set AMZN_TEST_LOCALDB=1 to run these tests against SQL Server LocalDB.";
        }
    }
}
