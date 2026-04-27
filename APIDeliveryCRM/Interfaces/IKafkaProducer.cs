namespace APIDeliveryCRM.Interfaces;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, object payload, string? key = null, CancellationToken cancellationToken = default);
}
