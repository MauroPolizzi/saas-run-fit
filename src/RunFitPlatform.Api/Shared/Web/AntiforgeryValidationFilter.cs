using Microsoft.AspNetCore.Antiforgery;

namespace RunFitPlatform.Api.Shared.Web;

/// <summary>
/// Valida el token antiforgery (cabecera <c>X-XSRF-TOKEN</c>) en POST, PUT, PATCH y DELETE de todo el grupo
/// <c>/api</c>, login incluido. El middleware de antiforgery solo valida automáticamente endpoints con
/// formularios; los endpoints JSON necesitan este filtro.
/// </summary>
public sealed class AntiforgeryValidationFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var method = context.HttpContext.Request.Method;
        var modifiesState = HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

        if (modifiesState && !await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Token antiforgery inválido o ausente",
                extensions: new Dictionary<string, object?> { [ErrorCodes.ExtensionName] = ErrorCodes.AntiforgeryInvalid });
        }

        return await next(context);
    }
}
