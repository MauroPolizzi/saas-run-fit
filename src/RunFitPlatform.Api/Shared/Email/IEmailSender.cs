namespace RunFitPlatform.Api.Shared.Email;

/// <summary>Envío de emails transaccionales (invitaciones, recuperación de contraseña).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>Email en texto plano y HTML. Nunca debe contener datos de salud.</summary>
public sealed record EmailMessage(string To, string Subject, string TextBody, string? HtmlBody = null);

/// <summary>Enmascara emails para los logs: <c>mauro@gmail.com</c> → <c>m***@gmail.com</c>.</summary>
public static class EmailMasking
{
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***";
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0)
        {
            return "***";
        }

        return string.Concat(email.AsSpan(0, 1), "***", email.AsSpan(at));
    }
}
