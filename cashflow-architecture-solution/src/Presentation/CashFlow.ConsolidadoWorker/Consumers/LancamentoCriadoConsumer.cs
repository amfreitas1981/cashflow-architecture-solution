using System.Text;
using System.Text.Json;
using CashFlow.Application.Events;
using CashFlow.Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace CashFlow.ConsolidadoWorker.Consumers;

public class LancamentoCriadoConsumer : BackgroundService
{
    private readonly ILogger<LancamentoCriadoConsumer> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly string _hostname = "localhost";
    private readonly string _queueName = "lancamentos_queue";

    public LancamentoCriadoConsumer(ILogger<LancamentoCriadoConsumer> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory() { HostName = _hostname };
        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _queueName, 
            durable: true, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var @event = JsonSerializer.Deserialize<LancamentoCriadoEvent>(message);

            if (@event != null)
            {
                await ProcessarConsolidadoAsync(@event);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        };

        await channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessarConsolidadoAsync(LancamentoCriadoEvent @event)
    {
        var db = _redis.GetDatabase();
        var cacheKey = $"consolidado:comerciante:{@event.ComercianteId}:data:{@event.DataHora:yyyy-MM-dd}";

        var valorAjustado = @event.Tipo == TipoLancamento.Credito ? @event.Valor : -@event.Valor;

        var novoSaldo = await db.StringIncrementAsync(cacheKey, (double)valorAjustado);

        _logger.LogInformation("Consolidado Atualizado. Comerciante: {ComercianteId} | Novo Saldo: {Saldo}", 
            @event.ComercianteId, novoSaldo);
    }
}