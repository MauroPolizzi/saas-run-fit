using System.Collections.Concurrent;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Options;

namespace RunFitPlatform.Api.Shared.Email;

/// <summary>Envío por SMTP sin autenticación, pensado para Mailpit en desarrollo local.</summary>
public sealed partial class MailpitEmailSender(IOptions<EmailOptions> options, ILogger<MailpitEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var settings = options.Value;

        using var mail = new MailMessage
        {
            From = new MailAddress(settings.From, settings.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false,
        };
        mail.To.Add(message.To);

        if (message.HtmlBody is not null)
        {
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, MediaTypeNames.Text.Html));
        }

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort) { EnableSsl = false };
        await client.SendMailAsync(mail, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            LogSent(logger, EmailMasking.Mask(message.To));
        }
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Email enviado a {Recipient} por Mailpit")]
    private static partial void LogSent(ILogger logger, string recipient);
}

/// <summary>Guarda los emails en memoria. Solo para tests (entorno Testing).</summary>
public sealed class InMemoryEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyCollection<EmailMessage> Messages => _messages.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Clear() => _messages.Clear();
}

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddAppEmail(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryEmailSender>();
        services.AddScoped<MailpitEmailSender>();
        services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<IOptions<EmailOptions>>().Value.Provider switch
        {
            EmailProvider.Mailpit => sp.GetRequiredService<MailpitEmailSender>(),
            EmailProvider.InMemory => sp.GetRequiredService<InMemoryEmailSender>(),
            var provider => throw new InvalidOperationException($"Proveedor de email no soportado: {provider}."),
        });
        return services;
    }
}
