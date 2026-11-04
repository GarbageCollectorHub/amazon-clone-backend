using AMZN.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Data;

public class DataContext: DbContext
{
    public DbSet<User> Users { get; private set; } = null!;
    public DbSet<UserRefreshToken> UserRefreshTokens { get; private set; } = null!;

    public DbSet<Product> Products { get; private set; } = null!;
    public DbSet<ProductImage> ProductImages { get; private set; } = null!;
    public DbSet<ProductRating> ProductRatings { get; private set; } = null!;
    public DbSet<ProductReview> ProductReviews { get; private set; } = null!;
    public DbSet<ProductQuestion> ProductQuestions { get; private set; } = null!;
    public DbSet<ProductQuestionAnswer> ProductQuestionAnswers { get; private set; } = null!;

    public DbSet<Category> Categories { get; private set; } = null!;
    public DbSet<Promotion> Promotions { get; private set; } = null!;
    public DbSet<Brand> Brands { get; private set; } = null!;

    public DbSet<ProductPromotion> ProductPromotions { get; private set; } = null!;
    
    public DbSet<CategoryPromotion> CategoryPromotions { get; private set; } = null!;
    
    public DbSet<DeletedUser>  DeletedUsers { get; private set; } = null!;
    
    public DbSet<PaymentMethod> PaymentMethods { get; private set; } = null!;
    
    public DbSet<DeliveryAddress> DeliveryAddresses { get; private set; } = null!;
    
    public DbSet<Cart> Carts { get; private set; } = null!;
    
    public DbSet<CartItem> CartItems { get; private set; } = null!;
    
    public DbSet<Order> Orders { get; private set; } = null!;
    
    public DbSet<OrderItem> OrderItems { get; private set; } = null!;

    public DataContext(DbContextOptions options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("AMZN");


        modelBuilder.Entity<User>(u =>
        {
            u.HasIndex(x => x.Email)
                .IsUnique();

            
            u.Property(x => x.Role)
                .HasConversion<string>()   // храним enum Role как строку в БД (Admin/User), а не как int (0/1)
                .HasMaxLength(32)
                .IsRequired();
        });


        modelBuilder.Entity<UserRefreshToken>(u =>
        {
            u.HasIndex(x => x.TokenHash)
                .IsUnique();

            u.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Product>(p =>
        {
            p.HasIndex(x => new { x.CategoryId, x.CreatedAt });       
            p.HasIndex(x => x.BrandId);
            p.HasIndex(x => x.SellerId);
            p.HasIndex(x => x.Title);

            p.Property(x => x.Title)
                .HasMaxLength(256)
                .IsRequired();

            p.Property(x => x.PrimaryImageUrl)
                .HasMaxLength(2048)
                .IsRequired();

            p.Property(x => x.CurrentPrice)
                .HasColumnType("decimal(18,2)");

            p.Property(x => x.OriginalPrice)
                .HasColumnType("decimal(18,2)");

            p.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            p.HasOne(x => x.Brand)
                .WithMany(b  => b.Products)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            p.HasOne(x => x.Seller)
                .WithMany(u => u.Products)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            p.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Product_CurrentPrice", "CurrentPrice >= 0");

                t.HasCheckConstraint("CK_Product_OriginalPrice",
                    "OriginalPrice IS NULL OR OriginalPrice >= CurrentPrice");
                t.HasCheckConstraint("CK_Product_RatingSum", "RatingSum >= 0");
                t.HasCheckConstraint("CK_Product_RatingCount", "RatingCount >= 0");
                t.HasCheckConstraint("CK_Product_StockQuantity", "StockQuantity >= 0");
            });
        });

        modelBuilder.Entity<ProductImage>(i =>
        {
            i.HasIndex(x => x.ProductId);

            i.Property(x => x.Url)
                .HasMaxLength(2048)
                .IsRequired();

            i.HasOne(x => x.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            i.HasIndex(x => new { x.ProductId, x.SortOrder })
                .IsUnique();

            i.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ProductImage_SortOrder", "SortOrder >= 0");
            });
        });


        modelBuilder.Entity<ProductRating>(r =>
        {
            r.HasIndex(x => new { x.ProductId, x.UserId })
                .IsUnique();

            r.Property(x => x.Value)
                .IsRequired();

            r.HasOne(x => x.Product)
                .WithMany(p => p.Ratings)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            r.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            r.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ProductRating_Value", "Value >= 1 AND Value <= 5");
            });
        });


        modelBuilder.Entity<ProductReview>(r =>
        {
            r.HasIndex(x => new { x.ProductId, x.UserId })
                .IsUnique();

            r.HasIndex(x => new { x.ProductId, x.CreatedAt });

            r.Property(x => x.Title)
                .HasMaxLength(120)
                .IsRequired();

            r.Property(x => x.Text)
                .HasMaxLength(4000)
                .IsRequired();

            r.HasOne(x => x.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            r.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<ProductQuestion>(q =>
        {
            q.HasIndex(x => new { x.ProductId, x.CreatedAt });

            q.Property(x => x.Text)
                .HasMaxLength(1024)
                .IsRequired();

            q.HasOne(x => x.Product)
                .WithMany(p => p.Questions)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            q.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<ProductQuestionAnswer>(a =>
        {
            a.HasIndex(x => new { x.QuestionId, x.CreatedAt });

            a.Property(x => x.Text)
                .HasMaxLength(2048)
                .IsRequired();

            a.HasOne(x => x.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            a.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });






        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany()
            .HasForeignKey(c => c.ParentId);
        
        modelBuilder.Entity<ProductPromotion>()
            .HasOne(pa => pa.Product)
            .WithMany()
            .HasForeignKey(pa => pa.ProductId)
            .HasPrincipalKey(p => p.Id);
        
        modelBuilder.Entity<ProductPromotion>()
            .HasOne(pa => pa.Promotion)
            .WithMany()
            .HasForeignKey(pa => pa.PromotionId)
            .HasPrincipalKey(a => a.Id);
        
        modelBuilder.Entity<CategoryPromotion>()
            .HasOne(ca => ca.Category)
            .WithMany()
            .HasForeignKey(ca => ca.CategoryId)
            .HasPrincipalKey(c => c.Id);
        
        modelBuilder.Entity<CategoryPromotion>()
            .HasOne(ca => ca.Promotion)
            .WithMany()
            .HasForeignKey(ca => ca.PromotionId)
            .HasPrincipalKey(a => a.Id);


        modelBuilder.Entity<Brand>(b =>
        {
            b.Property(x => x.Name)
                .HasMaxLength(128)
                .IsRequired();

            b.HasIndex(x => x.Name)
                .IsUnique();
        });
        
        modelBuilder.Entity<PaymentMethod>()
            .HasOne(p => p.User)
            .WithMany(u => u.PaymentMethods)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DeliveryAddress>()
            .HasOne(d => d.User)
            .WithMany(u => u.DeliveryAddresses)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cart>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .HasPrincipalKey(u => u.Id);

        modelBuilder.Entity<Order>(o =>
        {
            o.HasOne(x => x.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(x => x.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);

            o.HasOne(x => x.DeliveryAddress)
                .WithMany()
                .HasForeignKey(x => x.DeliveryAddressId)
                .OnDelete(DeleteBehavior.NoAction);

            o.HasOne(x => x.PaymentMethod)
                .WithMany()
                .HasForeignKey(x => x.PaymentMethodId)
                .OnDelete(DeleteBehavior.NoAction);

            o.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

            o.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();
        });
        
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        /*
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.NoAction);
        */

        modelBuilder.Entity<OrderItem>()
            .Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");
    }
}
