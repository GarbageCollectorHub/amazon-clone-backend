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

        public static IServiceCollection AddAmznRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Наш ApiErrorResponse JSON response вместо "пустого 429"
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var http = context.HttpContext;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    {
                        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                        http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                    }

                    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == ImageUploadsPolicy)
                    {
                        var result = new ViewResult
                        {
                            ViewName = "~/Views/Shared/UploadLimit.cshtml",
                            StatusCode = StatusCodes.Status429TooManyRequests,
                            ViewData = new ViewDataDictionary(
                                http.RequestServices.GetRequiredService<IModelMetadataProvider>(), new ModelStateDictionary())
                        };
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

                options.AddPolicy(ImageUploadsPolicy, context =>
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var key = userId != null ? $"user:{userId}" : $"ip:{context.Connection.RemoteIpAddress}";
                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
                });

                options.AddPolicy("Auth", context =>
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


        public static WebApplication UseAmznRateLimiting(this WebApplication app)
        {
            app.UseRateLimiter();
            return app;
        }
    }

}

