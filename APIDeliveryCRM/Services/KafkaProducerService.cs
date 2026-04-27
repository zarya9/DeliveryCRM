using System.Text.Json;
using APIDeliveryCRM.Interfaces;
using Confluent.Kafka;

namespace APIDeliveryCRM.Services;

public sealed class KafkaProducerService : IKafkaProducer, IDisposable
{
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IProducer<string, string>? _producer;
    private readonly bool _enabled;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _enabled = false;
            _logger.LogInformation("Kafka producer disabled: Kafka:BootstrapServers is empty.");
            return;
        }

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = configuration["Kafka:ClientId"] ?? "apideliverycrm-api",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 10000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _enabled = true;
        _logger.LogInformation("Kafka producer initialized for {BootstrapServers}.", bootstrapServers);
    }

    public async Task ProduceAsync(string topic, object payload, string? key = null, CancellationToken cancellationToken = default)
    {
        if (!_enabled || _producer is null)
            return;
        if (string.IsNullOrWhiteSpace(topic))
            return;

        try
        {
            var message = JsonSerializer.Serialize(payload);
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = string.IsNullOrWhiteSpace(key) ? Guid.NewGuid().ToString("N") : key,
                Value = message
            }, cancellationToken);

            _logger.LogDebug("Kafka message delivered. Topic={Topic}, Partition={Partition}, Offset={Offset}",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka publish failed for topic {Topic}.", topic);
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(2));
            _producer?.Dispose();
        }
        catch
        {
            // ignore shutdown errors
        }
    }
}
