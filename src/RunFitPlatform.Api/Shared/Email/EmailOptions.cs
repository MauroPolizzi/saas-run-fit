using System.ComponentModel.DataAnnotations;

namespace RunFitPlatform.Api.Shared.Email;

public enum EmailProvider
{
    /// <summary>Sin valor: la aplicación no arranca hasta configurarlo.</summary>
    None = 0,

    /// <summary>SMTP hacia Mailpit (desarrollo local): captura los emails, no los envía.</summary>
    Mailpit,

    /// <summary>Guarda los emails en memoria para leerlos desde los tests.</summary>
    InMemory,
}

/// <summary>Envío de emails (sección <c>Email</c>). El proveedor de producción se define junto con D6.</summary>
public sealed class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    public EmailProvider Provider { get; set; }

    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = "Run Fit Platform";

    public string SmtpHost { get; set; } = "localhost";

    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 1025;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Provider == EmailProvider.None)
        {
            yield return new ValidationResult("Email:Provider es obligatorio.", [nameof(Provider)]);
        }

        if (Provider == EmailProvider.Mailpit && string.IsNullOrWhiteSpace(SmtpHost))
        {
            yield return new ValidationResult("Email:SmtpHost es obligatorio con Mailpit.", [nameof(SmtpHost)]);
        }
    }
}
