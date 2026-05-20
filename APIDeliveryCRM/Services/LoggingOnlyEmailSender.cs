using APIDeliveryCRM.Interfaces;

namespace APIDeliveryCRM.Services;

/// <summary>Используется при Smtp:Enabled=false: в Development пишет тело в лог, иначе только факт.</summary>
public sealed class LoggingOnlyEmailSender : IEmailSender
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LoggingOnlyEmailSender> _logger;

    public LoggingOnlyEmailSender(IWebHostEnvironment environment, ILogger<LoggingOnlyEmailSender> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string plainTextBody, CancellationToken cancellationToken = default)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogWarning(
                "SMTP отключён (Smtp:Enabled=false). Письмо не отправлено. Получатель: {To}, тема: {Subject}. Текст:\n{Body}",
                toEmail, subject, plainTextBody);
        }
        else
        {
            _logger.LogWarning(
                "SMTP отключён: письмо для {To} с темой «{Subject}» не отправлено. Включите Smtp:Enabled и настройте хост.",
                toEmail, subject);
        }

        return Task.CompletedTask;
    }
}
