using AMZN.DTOs.Common;
using AMZN.Shared.Exceptions.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace AMZN.Extensions
{
    public static class RateLimitingExtensions
    {
        public const string ImageUploadsPolicy = "ImageUploads";
        public const string CatalogWritesPolicy = "CatalogWrites";
        public const string LoginPolicy = "Login";
        public const string RegistrationPolicy = "Registration";
        public const string RefreshPolicy = "Refresh";
        public const string AccountApiPolicy = "Auth";

        public static IServiceCollection AddAmznRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // MVC forms receive HTML; API and AJAX requests receive JSON.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var http = context.HttpContext;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    {
                        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                        http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                    }

                    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    string? policy = http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                    bool isMvcRegistration = policy == RegistrationPolicy && !http.Request.Path.StartsWithSegments("/api");
                    if (policy is ImageUploadsPolicy or CatalogWritesPolicy || isMvcRegistration)
                    {
                        var result = new ViewResult
                        {
                            ViewName = "~/Views/Shared/RateLimit.cshtml",
                            StatusCode = StatusCodes.Status429TooManyRequests,
                            ViewData = new ViewDataDictionary(
                                http.RequestServices.GetRequiredService<IModelMetadataProvider>(), new ModelStateDictionary())
                        };
                        result.ViewData["LimitMessage"] = policy switch
                        {
                            RegistrationPolicy => "You have submitted too many registration requests.",
                            CatalogWritesPolicy => "You have submitted too many brand or promotion requests.",
                            _ => "You have submitted several product or category changes too quickly."
                        };
                        result.ViewData["RetryAfterMinutes"] = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
                        await result.ExecuteResultAsync(new ActionContext(http, http.GetRouteData(), new ActionDescriptor()));
                        return;
                    }

                    http.Response.ContentType = "application/json; charset=utf-8";

                    var payload = new ApiErrorResponse
                    {
                        Code = ErrorCodes.TooManyRequests,
                        Message = "Too many requests",
                        TraceId = http.TraceIdentifier,
                        Errors = null
                    };

                    await http.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web), cancellationToken);
                };

                AddIpPolicy(options, LoginPolicy, 5, TimeSpan.FromMinutes(1));
                AddIpPolicy(options, RegistrationPolicy, 3, TimeSpan.FromMinutes(10));
                AddIpPolicy(options, RefreshPolicy, 30, TimeSpan.FromMinutes(1));

                AddAccountPolicy(options, ImageUploadsPolicy, 10);
                AddAccountPolicy(options, CatalogWritesPolicy, 10);

                options.AddPolicy(AccountApiPolicy, context =>
                {
                    // При деплое за прокси(Azure/AWS/K8s) RemoteIpAddress может приходить в X-Forwarded-For.
                    // Тогда нужен ForwardedHeaders (см. Extensions/ForwardedHeadersExtensions).
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,                 // максимум 10 запросов
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,                   // не ставим запросы "в очередь", лишние сразу 429

                        // Если QueueLimit > 0, то это определяет порядок обработки очереди:
                        // OldestFirst - сначала самые старые запросы (справедливее для пользователей)
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
                });
            });

            return services;
        }


        private static void AddIpPolicy(RateLimiterOptions options, string name, int limit, TimeSpan window)
        {
            options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = window,
                    QueueLimit = 0
                }));
        }

        private static void AddAccountPolicy(RateLimiterOptions options, string name, int limit)
        {
            options.AddPolicy(name, context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var key = userId != null ? $"user:{userId}" : $"ip:{context.Connection.RemoteIpAddress}";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        }

        public static WebApplication UseAmznRateLimiting(this WebApplication app)
        {
            app.UseRateLimiter();
            return app;
        }
    }

}

