using APIDeliveryCRM.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace APIDeliveryCRM.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string plainTextBody, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Smtp:Host is not configured.");

        var port = _configuration.GetValue("Smtp:Port", 587);
        var user = _configuration["Smtp:UserName"];
        var password = _configuration["Smtp:Password"];
        var fromAddress = _configuration["Smtp:FromAddress"];
        var fromName = _configuration["Smtp:FromName"] ?? "DeliveryCRM";

        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Smtp:FromAddress is not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = plainTextBody };

        using var client = new SmtpClient();
        client.Timeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;

        var useSsl = _configuration.GetValue("Smtp:UseSsl", false);
        // Порт 587 — обычно обязательный STARTTLS (Mail.ru, Gmail и др.). 465 — SSL с первого байта (UseSsl=true).
        var socketOptions = useSsl
            ? SecureSocketOptions.SslOnConnect
            : port == 587
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(host, port, socketOptions, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(user))
            await client.AuthenticateAsync(user, password ?? string.Empty, cancellationToken).ConfigureAwait(false);

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("SMTP: sent message to {To} subject {Subject}", toEmail, subject);
    }
}
