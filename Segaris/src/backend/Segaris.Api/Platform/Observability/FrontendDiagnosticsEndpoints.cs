using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Segaris.Api.Configuration;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Platform.Observability;

internal static partial class FrontendDiagnosticsEndpoints
{
    internal const string RateLimitPolicy = "frontend-diagnostics";
    public static void MapFrontendDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<DiagnosticsOptions>>().Value;
        var group = endpoints.MapSegarisApiGroup("diagnostics/frontend", "Frontend diagnostics")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);

        group.MapPost("", RecordAsync)
            .WithMetadata(new ApiRequestBodyLimit(options.MaxBodyBytes))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Records a bounded diagnostic event from the frontend");
    }

    private static IResult RecordAsync(
        FrontendDiagnosticRequest request,
        ICurrentUser currentUser,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        Validate(request);

        var logger = loggerFactory.CreateLogger("Segaris.FrontendDiagnostics");
        var level = Enum.Parse<LogLevel>(request.Severity!, ignoreCase: true);
        logger.Log(
            level,
            "Frontend diagnostic {EventCode}: {DiagnosticMessage}. UserId={UserId} Route={FrontendRoute} Component={Component} ClientTraceId={ClientTraceId} Stack={DiagnosticStack}",
            request.EventCode,
            FrontendDiagnosticRedactor.Redact(request.Message),
            currentUser.UserId!.Value.Value,
            request.Route,
            request.Component,
            request.ClientTraceId,
            FrontendDiagnosticRedactor.Redact(request.Stack));

        return TypedResults.Accepted(
            uri: (string?)null,
            value: new FrontendDiagnosticResponse(httpContext.TraceIdentifier));
    }

    private static void Validate(FrontendDiagnosticRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        ValidateRequired(request.EventCode, "eventCode", 64, errors);
        if (!string.IsNullOrWhiteSpace(request.EventCode) && !EventCodePattern().IsMatch(request.EventCode))
        {
            errors["eventCode"] = ["Event code may contain letters, digits, dots, underscores, and hyphens only."];
        }

        ValidateRequired(request.Message, "message", 500, errors);
        ValidateOptional(request.Stack, "stack", 4_000, errors);
        ValidateOptional(request.Route, "route", 256, errors);
        ValidateOptional(request.Component, "component", 128, errors);
        ValidateOptional(request.ClientTraceId, "clientTraceId", 128, errors);

        if (string.IsNullOrWhiteSpace(request.Severity)
            || !Enum.TryParse<LogLevel>(request.Severity, ignoreCase: true, out var severity)
            || severity is LogLevel.None or LogLevel.Trace or LogLevel.Debug)
        {
            errors["severity"] = ["Severity must be Information, Warning, Error, or Critical."];
        }

        if (errors.Count > 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: errors);
        }
    }

    private static void ValidateRequired(
        string? value,
        string field,
        int maximumLength,
        IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required."];
        }
        else if (value.Length > maximumLength)
        {
            errors[field] = [$"{field} must not exceed {maximumLength} characters."];
        }
    }

    private static void ValidateOptional(
        string? value,
        string field,
        int maximumLength,
        IDictionary<string, string[]> errors)
    {
        if (value?.Length > maximumLength)
        {
            errors[field] = [$"{field} must not exceed {maximumLength} characters."];
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EventCodePattern();

    internal sealed record FrontendDiagnosticRequest(
        string? EventCode,
        string? Severity,
        string? Message,
        string? Stack,
        string? Route,
        string? Component,
        string? ClientTraceId);

    internal sealed record FrontendDiagnosticResponse(string TraceId);
}
