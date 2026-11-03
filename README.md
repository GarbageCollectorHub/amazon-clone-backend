# AMZN

E-commerce API and Management Portal

An Amazon-inspired e-commerce backend built with ASP.NET Core, Entity Framework Core and SQL Server. It includes a REST API for a React storefront and a responsive MVC interface for managing the catalog.

The project was built by a team of three frontend and two backend developers using a shared Figma design. This repository contains the backend and MVC management interface. The React storefront is in a separate repository.

## My contribution

My main areas of responsibility were the backend API, data access and MVC product management.

- Built the main API layers using controllers, services, repositories and EF Core, with shared DTOs and centralized error handling.
- Developed the catalog API with category, brand, price and rating filters, pagination and multiple sorting options. Category selection includes subcategories and updates the available brand filters.
- Implemented catalog search and search suggestions for products, categories and brands, with more exact matches shown first.
- Built homepage APIs for product and category sections, including popular products and recently viewed items.
- Implemented JWT authentication with access and refresh tokens, BCrypt password hashing and rate limiting for authentication endpoints.
- Added product ratings, reviews and Q&A, with database transactions to protect concurrent rating and review updates.
- Built MVC product and brand management, image galleries and Azure Blob Storage integration, with reusable server-side image validation.
- Refactored homepage queries to apply filtering, grouping and limits in the database, and updated storage operations to use async calls.

## Project preview

Catalog browsing with category, brand, rating and price filters, plus sorting options.

<a href="docs/images/storefront-catalog.png">
  <img src="docs/images/storefront-catalog.png" alt="React storefront catalog with product filters and sorting" width="900">
</a>

The screenshots show the React storefront built by the frontend team. Its source code is in a separate repository.

Click any preview below to open the full-size screenshot.

<table>
  <tr>
    <th width="50%">Storefront search</th>
    <th width="50%">Management dashboard</th>
  </tr>
  <tr>
    <td valign="top">
      <a href="docs/images/storefront-search.png"><img src="docs/images/storefront-search.png" alt="Storefront search suggestions with matching products" width="420"></a><br>
      Product suggestions as the user types.<br>
      <a href="docs/images/storefront-search.png">Open full-size screenshot</a>
    </td>
    <td valign="top">
      <a href="docs/images/admin-dashboard-dark.png"><img src="docs/images/admin-dashboard-dark.png" alt="MVC management dashboard in dark mode" width="420"></a><br>
      Product and catalog management, with light and dark themes.<br>
      <a href="docs/images/admin-dashboard-dark.png">Open full-size screenshot</a>
    </td>
  </tr>
  <tr>
    <th width="50%">Product listings</th>
    <th width="50%">Product editor</th>
  </tr>
  <tr>
    <td valign="top">
      <a href="docs/images/admin-products.png"><img src="docs/images/admin-products.png" alt="MVC product listings with search, sorting, prices and stock levels in light mode" width="420"></a><br>
      Search and sort the seller's catalog.<br>
      <a href="docs/images/admin-products.png">Open full-size screenshot</a>
    </td>
    <td valign="top">
      <a href="docs/images/admin-product-edit.png"><img src="docs/images/admin-product-edit.png" alt="MVC product editor in dark mode with pricing, stock and a gallery of distinct product images" width="420"></a><br>
      Edit product details, pricing and image galleries.<br>
      <a href="docs/images/admin-product-edit.png">Open full-size screenshot</a>
    </td>
  </tr>
</table>

### Admin UI scenarios

See form errors, image previews and mobile layouts. Click a preview to open that section in the gallery.

<table>
  <tr>
    <th width="33%">Form validation</th>
    <th width="33%">Image previews</th>
    <th width="33%">Mobile layout</th>
  </tr>
  <tr>
    <td valign="top" align="center">
      <a href="docs/admin-ui.md#form-validation"><img src="docs/images/admin-form-validation.png" alt="Product form with inline validation messages" width="260"></a><br>
      <a href="docs/admin-ui.md#form-validation">View validation scenario</a>
    </td>
    <td valign="top" align="center">
      <a href="docs/admin-ui.md#image-previews"><img src="docs/images/admin-gallery-preview.png" alt="Product editor with a newly selected gallery image" width="260"></a><br>
      <a href="docs/admin-ui.md#image-previews">View gallery editing</a>
    </td>
    <td valign="top" align="center">
      <a href="docs/admin-ui.md#mobile-layouts"><img src="docs/images/admin-mobile-catalog.png" alt="Product listings on a mobile screen" width="106"></a><br>
      <a href="docs/admin-ui.md#mobile-layouts">View mobile layouts</a>
    </td>
  </tr>
  <tr>
    <td colspan="3" align="center">
      <strong><a href="docs/admin-ui.md">Browse the full admin UI gallery</a></strong><br>
      9 screenshots of the dashboard, catalog, editor, form errors, delete confirmation and mobile navigation.
    </td>
  </tr>
</table>

## Tech stack

- C# / .NET 9
- ASP.NET Core Web API and MVC with Razor views
- Entity Framework Core 9 and SQL Server
- JWT Bearer authentication and BCrypt password hashing
- Azure Blob Storage
- Swagger / OpenAPI

## Features

- Product catalog with category, brand, price, and rating filters.
- Product details, image galleries, search suggestions, and homepage collections.
- Customer ratings and reviews, with options to update them.
- Product questions and answers with seller-response identification.
- User profiles, delivery addresses, and demo payment-method records.
- Shopping cart, order creation, confirmation, cancellation, and order history.
- MVC management pages for products, categories, brands, and promotions, with light and dark themes.
- Scheduled promotion activation and expiration.

### MVC management interface

- A responsive dashboard and navigation for desktop and mobile screens.
- Product listings for the signed-in seller, with search and sorting by title, price or creation date.
- Product creation and editing with category, brand, pricing and stock fields, plus client-side and server-side validation.
- Image previews and drag-and-drop gallery uploads. Existing gallery images can be reordered or removed before saving.
- Product deletion confirmation and helpful empty search states.
- Light and dark themes that remember the user's choice, with account controls in the top bar.

## Architecture and implementation

The application uses controller, service and repository layers in a single ASP.NET Core project.

```text
Controllers/     API endpoints and MVC controllers
Services/        Business logic and background processing
Repositories/    Database queries and persistence
Data/            Entities and EF Core model configuration
DTOs/            API request and response contracts
Models/          MVC form and view models
Security/        Password hashing and JWT generation
Shared/          Storage, transactions, validation, and helpers
Middleware/      API exception handling
Views/           Razor management interface
Migrations/      SQL Server database migrations
```

### Authentication

The API and MVC interface use the same JWT authentication. API clients can send access tokens in the `Authorization: Bearer` header. Browser clients can also use the `amzn_access` cookie. If both are present, the header is used.

Refresh tokens are stored as hashes. When a refresh token is used, the old token is revoked and a new one is created in the same database transaction.

### Database consistency

Rating and review updates use database transactions and SQL Server locking hints (`UPDLOCK`, `ROWLOCK`). This makes updates to the same product run one at a time. Unique indexes limit each user to one rating and one review per product.

Adding delivery addresses and payment methods uses transactions and per-user locks. These protect checks for duplicates and the choice of a default address or payment method.

### Database diagram

The SQL Server schema includes 20 application tables and 27 foreign-key relationships. Click the diagram to view it at full size.

<a href="docs/images/database-schema.png">
  <img src="docs/images/database-schema.png" alt="SQL Server database tables, columns and relationships" width="1000">
</a>

**[Open full-size database diagram](docs/images/database-schema.png)**

[Schema details and notation](docs/database-schema.md)

### Queries and background processing

Question previews show up to three answers per question. The database query handles sorting and the three-answer limit.

A background service checks promotion dates at startup and every minute while the application is running. It applies or removes discounts based on their start and end dates.

## API explorer

Swagger UI documents the API and supports authenticated requests through its **Authorize** dialog.

Click a preview or its **Open full size** link to read the endpoint list.

<table>
  <tr>
    <th width="33%">Products and community</th>
    <th width="33%">Authentication and management</th>
    <th width="33%">Cart and orders</th>
  </tr>
  <tr>
    <td valign="top">
      <a href="docs/images/swagger-products.png"><img src="docs/images/swagger-products.png" alt="Product catalog, reviews, questions and search endpoints" width="280"></a><br>
      <a href="docs/images/swagger-products.png">Open full size</a>
    </td>
    <td valign="top">
      <a href="docs/images/swagger-auth-management.png"><img src="docs/images/swagger-auth-management.png" alt="Authentication and catalog management endpoints" width="280"></a><br>
      <a href="docs/images/swagger-auth-management.png">Open full size</a>
    </td>
    <td valign="top">
      <a href="docs/images/swagger-cart-orders.png"><img src="docs/images/swagger-cart-orders.png" alt="Cart, categories, homepage and order endpoints" width="280"></a><br>
      <a href="docs/images/swagger-cart-orders.png">Open full size</a>
    </td>
  </tr>
</table>

## Running locally

### Prerequisites

- .NET 9 SDK
- SQL Server, or SQL Server Express LocalDB on Windows
- Entity Framework Core CLI tools matching the project's EF Core version
- An Azure Storage account to enable image uploads (optional for browsing the application)

If the EF Core CLI is not installed:

```powershell
dotnet tool install --global dotnet-ef --version 9.0.12
```

### 1. Configure the application

Create `appsettings-Secrets.json` in the project root:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AMZN_DB;Trusted_Connection=True;TrustServerCertificate=True;",
    "AzureStorage": ""
  },
  "Jwt": {
    "Key": "<Base64-encoded random signing key>",
    "Issuer": "AMZN",
    "Audience": "AMZN.Client",
    "ExpiresMinutes": 30,
    "RefreshDays": 7
  }
}
```

The SQL Server connection string above uses Windows LocalDB. Replace it with your own connection string if using another SQL Server instance.

`Jwt:Key` must contain at least 32 random bytes encoded as Base64. Generate a key in PowerShell:

```powershell
$keyBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($keyBytes)
[Convert]::ToBase64String($keyBytes)
$rng.Dispose()
```

Copy the generated value into `Jwt:Key`.

`appsettings-Secrets.json` is excluded from Git. Keep credentials and signing keys out of committed configuration files.

### 2. Restore dependencies and apply migrations

Run from the project root:

```powershell
dotnet restore
dotnet ef database update
```

### 3. Start the application

Open `AMZN.sln` in Visual Studio, select the `https` profile and run the project.

Or run it from the terminal:

```powershell
dotnet run --launch-profile https
```

If the browser warns about the local HTTPS certificate, trust the development certificate once:

```powershell
dotnet dev-certs https --trust
```

Local URLs:

- [MVC interface](https://localhost:7202/)
- [Swagger UI](https://localhost:7202/swagger)
- [OpenAPI document](https://localhost:7202/swagger/v1/swagger.json)

Swagger is enabled in the Development environment.

### 4. Enable image uploads

To enable uploads, set `ConnectionStrings:AzureStorage` to your Azure Storage connection string and restart the application. The `amzn-storage` container is created automatically on the first image upload if it does not already exist.

Catalog images use public blob URLs. The storage account must allow anonymous blob access. If you use an existing container, enable public read access for its blobs.

You can leave `AzureStorage` empty to explore the API and MVC pages without image uploads. If you try to upload an image, the app shows an error message. Browsing product lists does not require a server-side connection to Blob Storage.

### 5. Explore the project

For API access, register through `POST /api/auth/register` or sign in through `POST /api/auth/login`. Copy the returned `accessToken` into the Swagger **Authorize** dialog.

For a local management demo, create an account at `/Admin/SignUp`, select the `Admin` role, and sign in through the MVC interface. Create categories and brands before adding products. Creating categories and products with images requires configured Blob Storage.

A new database starts without catalog data. Use test account and payment details when trying the demo features.
