using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RunFitPlatform.Api.Shared.Email;
using RunFitPlatform.Api.Shared.Security;
using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.UnitTests.Shared;

public sealed class DataScopeTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void Resolve_Anonymous_HasNoScope()
    {
        var scope = HttpDataScope.Resolve(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Null(scope.UserId);
        Assert.Null(scope.CoachId);
        Assert.Null(scope.AthleteId);
        Assert.False(scope.IsPlatformAdmin);
    }

    [Fact]
    public void Resolve_Coach_CoachIdIsUserId()
    {
        var scope = HttpDataScope.Resolve(User(Roles.Coach));

        Assert.Equal(UserId, scope.CoachId);
        Assert.Null(scope.AthleteId);
        Assert.False(scope.IsPlatformAdmin);
    }

    [Fact]
    public void Resolve_Athlete_AthleteIdIsUserId()
    {
        var scope = HttpDataScope.Resolve(User(Roles.Athlete));

        Assert.Equal(UserId, scope.AthleteId);
        Assert.Null(scope.CoachId);
    }

    [Fact]
    public void Resolve_PlatformAdmin_HasNoCoachOrAthleteScope()
    {
        var scope = HttpDataScope.Resolve(User(Roles.PlatformAdmin));

        Assert.True(scope.IsPlatformAdmin);
        Assert.Null(scope.CoachId);
        Assert.Null(scope.AthleteId);
    }

    private static ClaimsPrincipal User(string role) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, UserId.ToString()), new Claim(ClaimTypes.Role, role)],
        authenticationType: "Test"));
}

public sealed class EmailMaskingTests
{
    [Theory]
    [InlineData("mauro@gmail.com", "m***@gmail.com")]
    [InlineData("a@b.com", "a***@b.com")]
    [InlineData("sin-arroba", "***")]
    [InlineData("", "***")]
    [InlineData(null, "***")]
    public void Mask_HidesLocalPart(string? email, string expected) =>
        Assert.Equal(expected, EmailMasking.Mask(email));
}

public sealed class ErrorHandlingTests
{
    [Theory]
    [InlineData(400, ErrorCodes.ValidationFailed)]
    [InlineData(401, ErrorCodes.Unauthenticated)]
    [InlineData(403, ErrorCodes.Forbidden)]
    [InlineData(404, ErrorCodes.NotFound)]
    [InlineData(409, ErrorCodes.ConcurrencyConflict)]
    [InlineData(429, ErrorCodes.RateLimitExceeded)]
    [InlineData(500, ErrorCodes.ServerError)]
    [InlineData(503, ErrorCodes.ServerError)]
    public void FromStatusCode_ReturnsDefaultCode(int status, string expected) =>
        Assert.Equal(expected, ErrorCodes.FromStatusCode(status));

    [Fact]
    public void Classify_ConcurrencyException_Returns409() =>
        Assert.Equal((409, ErrorCodes.ConcurrencyConflict), GlobalExceptionHandler.Classify(new DbUpdateConcurrencyException()));

    [Fact]
    public void Classify_UnexpectedException_Returns500WithoutDetails() =>
        Assert.Equal((500, ErrorCodes.ServerError), GlobalExceptionHandler.Classify(new InvalidOperationException("detalle interno")));
}

public sealed class AntiforgeryValidationFilterTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task InvokeAsync_StateChangingWithoutValidToken_Returns400(string method)
    {
        var filter = new AntiforgeryValidationFilter(new FakeAntiforgery(isValid: false));
        var (context, nextCalled) = CreateContext(method);

        var result = await filter.InvokeAsync(context, _ => { nextCalled.Value = true; return ValueTask.FromResult<object?>(null); });

        var problem = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>(result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(ErrorCodes.AntiforgeryInvalid, problem.ProblemDetails.Extensions[ErrorCodes.ExtensionName]);
        Assert.False(nextCalled.Value);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task InvokeAsync_SafeMethod_DoesNotValidate(string method)
    {
        var filter = new AntiforgeryValidationFilter(new FakeAntiforgery(isValid: false));
        var (context, nextCalled) = CreateContext(method);

        await filter.InvokeAsync(context, _ => { nextCalled.Value = true; return ValueTask.FromResult<object?>(null); });

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task InvokeAsync_PostWithValidToken_CallsNext()
    {
        var filter = new AntiforgeryValidationFilter(new FakeAntiforgery(isValid: true));
        var (context, nextCalled) = CreateContext("POST");

        await filter.InvokeAsync(context, _ => { nextCalled.Value = true; return ValueTask.FromResult<object?>(null); });

        Assert.True(nextCalled.Value);
    }

    private static (EndpointFilterInvocationContext Context, StrongBox<bool> NextCalled) CreateContext(string method)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        return (new DefaultEndpointFilterInvocationContext(http), new StrongBox<bool>());
    }

    private sealed class StrongBox<T>
    {
        public T? Value { get; set; }
    }

    private sealed class FakeAntiforgery(bool isValid) : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => throw new NotSupportedException();
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => throw new NotSupportedException();
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(isValid);
        public Task ValidateRequestAsync(HttpContext httpContext) => throw new NotSupportedException();
        public void SetCookieTokenAndHeader(HttpContext httpContext) => throw new NotSupportedException();
    }
}

public sealed class OptionsValidationTests
{
    [Fact]
    public void EmailOptions_WithoutProvider_IsInvalid()
    {
        var options = new EmailOptions { From = "no-reply@runfit.local" };

        Assert.Contains(Validate(options), r => r.MemberNames.Contains(nameof(EmailOptions.Provider)));
    }

    [Fact]
    public void EmailOptions_Mailpit_IsValid() =>
        Assert.Empty(Validate(new EmailOptions { Provider = EmailProvider.Mailpit, From = "no-reply@runfit.local" }));

    [Fact]
    public void RateLimitingOptions_Defaults_AreValid() =>
        Assert.Empty(Validate(new RateLimitingOptions()));

    [Fact]
    public void RateLimitingOptions_ZeroPermits_IsInvalid()
    {
        var options = new RateLimitingOptions { Login = new RateLimitRule { PermitLimit = 0, WindowSeconds = 60 } };

        Assert.Contains(Validate(options), r => r.MemberNames.Contains("Login.PermitLimit"));
    }

    private static List<ValidationResult> Validate(object options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}

public sealed class MaxLengthPasswordValidatorTests
{
    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public async Task ValidateAsync_EnforcesMaximumLength(int length, bool expectedValid)
    {
        var result = await new MaxLengthPasswordValidator().ValidateAsync(null!, new AppUser(), new string('x', length));

        Assert.Equal(expectedValid, result.Succeeded);
    }
}
