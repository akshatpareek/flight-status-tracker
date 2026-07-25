using FlightStatus.Api.Common;
using FlightStatus.Api.Contracts;
using FlightStatus.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace FlightStatus.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    private static readonly JsonSerializerOptions CamelCaseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string requestTraceId = httpContext.TraceIdentifier;
        string requestPath = httpContext.Request.Path;
        string httpMethod = httpContext.Request.Method;

        (int statusCode, string userFacingMessage) = MapExceptionToHttpResponse(exception);

        LogException(exception, httpMethod, requestPath, requestTraceId, statusCode);

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = AppConstants.ContentTypeJson;

        bool isRunningInDevelopment = IsRunningInDevelopment(httpContext);

        ErrorResponse errorResponse = new(
            TraceId: requestTraceId,
            StatusCode: statusCode,
            Message: userFacingMessage,
            Detail: isRunningInDevelopment ? exception.ToString() : null);

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, CamelCaseJsonOptions),
            cancellationToken);

        return true;
    }

    private static (int StatusCode, string UserFacingMessage) MapExceptionToHttpResponse(Exception exception) =>
        exception switch
        {
            OperationCanceledException or TaskCanceledException =>
                (AppConstants.HttpStatusClientClosedRequest,
                 AppConstants.ErrorMessages.ClientCancelledRequest),

            ProviderException providerException =>
                (StatusCodes.Status502BadGateway,
                 AppConstants.ErrorMessages.ProviderFailedToRespond(providerException.ProviderName)),

            ResourceNotFoundException =>
                (StatusCodes.Status500InternalServerError,
                 AppConstants.ErrorMessages.ServerResourceMissing),

            FlightStatusException flightStatusException =>
                (StatusCodes.Status500InternalServerError,
                 flightStatusException.Message),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 AppConstants.ErrorMessages.UnexpectedServerError)
        };

    private void LogException(
        Exception exception,
        string httpMethod,
        string requestPath,
        string requestTraceId,
        int statusCode)
    {
        string exceptionTypeName = exception.GetType().Name;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                AppConstants.LogMessages.ExceptionHandlerUnhandled,
                exceptionTypeName, httpMethod, requestPath, requestTraceId, statusCode);
        }
        else
        {
            _logger.LogWarning(
                exception,
                AppConstants.LogMessages.ExceptionHandlerHandled,
                exceptionTypeName, httpMethod, requestPath, requestTraceId, statusCode);
        }
    }

    private static bool IsRunningInDevelopment(HttpContext httpContext) =>
        httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();
}
