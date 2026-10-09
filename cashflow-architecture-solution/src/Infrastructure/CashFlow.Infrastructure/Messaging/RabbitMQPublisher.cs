using System.Text;
using System.Text.Json;
using CashFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace CashFlow.Infrastructure.Messaging;

public class RabbitMQPublisher : IEventPublisher
{
    private readonly string _hostname;
    private readonly string _queueName = "lancamentos_queue";

    public RabbitMQPublisher(IConfiguration configuration)
    {
        _hostname = configuration["RabbitMQ:Host"] ?? "localhost";
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : class
    {
        var factory = new ConnectionFactory() { HostName = _hostname };
        using var connection = await factory.CreateConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _queueName, 
            durable: true, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null,
            cancellationToken: cancellationToken
        );

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            mandatory: true,
            body: body,
            cancellationToken: cancellationToken
        );
    }
}