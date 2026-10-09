using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashFlow.ConsolidadoWorker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<ConsolidadoWorkerService>();

        var host = builder.Build();
        await host.RunAsync();
    }
}

public sealed class ConsolidadoWorkerService : BackgroundService
{
    private readonly ILogger<ConsolidadoWorkerService> _logger;

    public ConsolidadoWorkerService(ILogger<ConsolidadoWorkerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Executando processamento do worker...");

            try
            {
                // TODO: inserir a lógica do processamento aqui
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a fila do worker.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Worker finalizado.");
    }
}
