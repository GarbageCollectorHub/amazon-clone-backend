using AMZN.Data;
using AMZN.Extensions;
using AMZN.Middleware;
using AMZN.Models;
using AMZN.Repositories.Actions;
using AMZN.Repositories.Brands;
using AMZN.Repositories.Categories;
using AMZN.Repositories.Home;
using AMZN.Repositories.Products;
using AMZN.Repositories.Products.Questions;
using AMZN.Repositories.Search;
using AMZN.Repositories.Users;
using AMZN.Security.Passwords;
using AMZN.Security.Tokens;
using AMZN.Services.Account;
using AMZN.Services.Admin;
using AMZN.Services.Auth;
using AMZN.Services.BrandService;
using AMZN.Services.Categories;
using AMZN.Services.Home;
using AMZN.Services.Products;
using AMZN.Services.Products.Questions;
using AMZN.Services.Search;
using AMZN.Services.Storage.Cloud;
using AMZN.Services.Storage.Local;
using AMZN.Shared.Transactions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Security.Claims;
using AMZN.Repositories.Carts;
using AMZN.Repositories.Orders;
using AMZN.Services.Carts;
using AMZN.Services.Orders;

var builder = WebApplication.CreateBuilder(args);


// Config (DB connection string, secrets)
builder.Configuration.AddJsonFile("appsettings-Secrets.json", optional: true, reloadOnChange: true);



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApiValidationErrors();      // единый формат 400 ответа для ошибок валидации DTO (ModelState)


//  --- Swagger configuration ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AMZN API",
        Version = "Swag v1"
    });

    c.ExampleFilters();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT access token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// examples
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

// -----------------------------------------------


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// MySQL DbContext

builder.Services.AddDbContext<DataContext>(options => options
    .UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mySqlOptions => mySqlOptions.CommandTimeout(30)
    ));


/*
builder.Services.AddDbContext<DataContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("LocalMS"),
        sqlOptions => sqlOptions.CommandTimeout(30)));
*/

//  Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// DI services
builder.Services.AddScoped<FormsValidators>();
builder.Services.AddScoped<DataAccessor>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductQuestionRepository, ProductQuestionRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductQuestionService>();

builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<HomeService>();

builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<AdminCategoryService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<AdminProductService>();
builder.Services.AddScoped<IActionRepository, ActionRepository>();
builder.Services.AddScoped<AdminActionService>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<BrandService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<IDeletedUserRepository, DeletedUserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<OrderService>();
// Search
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<SearchService>();
// Storage
builder.Services.AddSingleton<ILocalsStorageService, LocalStorageService>();
builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();

// EF TransactionManager
builder.Services.AddScoped<ITransactionManager, EfTransactionManager>();

builder.Services.AddAmznForwardedHeaders();
builder.Services.AddAmznRateLimiting();



// JWT auth configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        const int MinJwtKeyBytes = 32; // HS256(HMAC with SHA-256)  -> 256 bit secret

        var keyBase64 = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Jwt:Key must be a valid Base64 string.", ex);
        }

        if (keyBytes.Length < MinJwtKeyBytes)
            throw new InvalidOperationException($"Jwt:Key is too short. Need at least {MinJwtKeyBytes} bytes for HS256.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrWhiteSpace(context.Token))
                {
                    return Task.CompletedTask;
                }

                string? token = context.Request.Cookies["amzn_access"];

                if (!string.IsNullOrWhiteSpace(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddOutputCache();
builder.Services.AddAuthorization();


var app = builder.Build();
app.UseAmznForwardedHeaders();
app.UseMiddleware<ApiExceptionMiddleware>();

// request logger
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await next();
        }
        finally
        {
            sw.Stop();

            app.Logger.LogInformation(
                "HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    });
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AMZN API v1");
        c.RoutePrefix = "swagger";
    });

}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Action validity check + cart creation
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var checker = services.GetRequiredService<AdminActionService>();
    checker.CheckActionsValidity();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var cartChecker = services.GetRequiredService<CartService>();
    //await cartChecker.CreateCartsIfNotExist();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Для MVC перенаправляем 401/403 на кастомные страницы ошибок
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseStatusCodePagesWithReExecute("/Home/AuthStatus", "?code={0}");
});

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseSession();
    appBuilder.UseAuthToken();
    appBuilder.UseAuthSession();
});

app.UseAuthorization();

app.UseAmznRateLimiting();

app.UseOutputCache();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();


app.Run();