using AMZN.DTOs.Common;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Middleware
{

    // Глобальный обработчик ошибок для API (route  /api/*)
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }


        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (IsApiRequest(context))
            {
                await HandleApiException(context, ex);
            }
        }


        private async Task HandleApiException(HttpContext context, Exception ex)
        {
            if (ex is ApiException apiEx)
            {
                _logger.LogWarning(
                    "API error: code={Code}, status={StatusCode}, traceId={TraceId}, message={Message}",
                    apiEx.Code,
                    apiEx.StatusCode,
                    context.TraceIdentifier,
                    apiEx.Message
                );

                await WriteErrorAsync(context, apiEx.StatusCode, new ApiErrorResponse
                {
                    Code = apiEx.Code,
                    Message = apiEx.Message,
                    TraceId = context.TraceIdentifier
                });
                return;
            }

            if (ex is DbUpdateException dbEx)
            {
                // Ошибка БД -> 500 (наружу не светим детали)
                _logger.LogError(dbEx,
                    "DbUpdateException: {Method} {Path}, traceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, new ApiErrorResponse
                {
                    Code = ErrorCodes.DatabaseError,
                    Message = "Internal server error",
                    TraceId = context.TraceIdentifier
                });
                return;
            }


            // Все остальные ошибки -> 500, детали клиенту не отдаём
            _logger.LogError(ex,
                "Unhandled exception: {Method} {Path}, traceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            // формирует ответ клиенту
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Code = ErrorCodes.InternalError,
                Message = "Internal server error",
                TraceId = context.TraceIdentifier
            });
        }



        private static bool IsApiRequest(HttpContext context)
        {
            // Для /api/* возвращаем JSON ошибки, для остальных роутов (MVC Razor) не трогаем
            return context.Request.Path.StartsWithSegments("/api");
        }


        private async Task WriteErrorAsync(HttpContext context, int statusCode, ApiErrorResponse error)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response has already started, cannot write error. traceId={TraceId}", context.TraceIdentifier);
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(error);
        }


    }
}
