namespace RunFitPlatform.Api.Shared.Web;

/// <summary>
/// Códigos transversales de <c>ProblemDetails</c>. Los módulos definen los suyos con el formato
/// <c>&lt;módulo&gt;.&lt;motivo&gt;</c> (por ejemplo <c>planning.relationship_inactive</c>).
/// Angular traduce cada código a un mensaje en español.
/// </summary>
public static class ErrorCodes
{
    public const string ExtensionName = "code";

    public const string ValidationFailed = "validation.failed";
    public const string Unauthenticated = "auth.unauthenticated";
    public const string Forbidden = "auth.forbidden";
    public const string AntiforgeryInvalid = "auth.antiforgery_invalid";
    public const string NotFound = "resource.not_found";
    public const string MethodNotAllowed = "request.method_not_allowed";
    public const string PayloadTooLarge = "request.too_large";
    public const string UnsupportedMediaType = "request.unsupported_media_type";
    public const string ConcurrencyConflict = "concurrency.conflict";
    public const string UniqueConflict = "conflict.duplicate";
    public const string RateLimitExceeded = "rate_limit.exceeded";
    public const string ServerError = "server.error";
    public const string BadRequest = "request.invalid";

    /// <summary>Código por defecto para respuestas que no traen uno propio.</summary>
    public static string FromStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ValidationFailed,
        StatusCodes.Status401Unauthorized => Unauthenticated,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
        StatusCodes.Status409Conflict => ConcurrencyConflict,
        StatusCodes.Status413PayloadTooLarge => PayloadTooLarge,
        StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
        StatusCodes.Status429TooManyRequests => RateLimitExceeded,
        >= 500 => ServerError,
        _ => BadRequest,
    };
}
