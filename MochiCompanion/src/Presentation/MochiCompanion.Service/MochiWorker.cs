using MochiCompanion.Application.Interfaces.ICommunication;
using MochiCompanion.Application.Interfaces.IServices;

namespace MochiCompanion.Service;

/// <summary>
/// Main background service that coordinates Mochi device communication and monitoring.
/// </summary>
public class MochiWorker : BackgroundService
{
    private readonly ILogger<MochiWorker> _logger;
    private readonly IConfiguration _config;
    private readonly IMochiConnection _connection;
    private readonly IMoodService _moodService;
    private readonly IMonitoringService _monitoringService;

    public MochiWorker(
        ILogger<MochiWorker> logger,
        IConfiguration config,
        IMochiConnection connection,
        IMoodService moodService,
        IMonitoringService monitoringService)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
        _moodService = moodService;
        _monitoringService = monitoringService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Mochi Companion Service starting...");

        try
        {
            // Connect to Mochi device
            await ConnectToMochiAsync(stoppingToken);

            // Start all monitors
            await _monitoringService.StartAsync(stoppingToken);

            _logger.LogInformation("All monitors started successfully");

            // Main loop - check for expired moods
            while (!stoppingToken.IsCancellationRequested)
            {
                await _moodService.CheckExpiryAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in service");
            throw;
        }
    }

    private async Task ConnectToMochiAsync(CancellationToken cancellationToken)
    {
        var connectionType = _config.GetValue<string>("Mochi:ConnectionType", "Serial");
        var address = _config.GetValue<string>("Mochi:Address", "COM3");
        var port = _config.GetValue<int>("Mochi:Port", 115200);

        _logger.LogInformation(
            "Attempting to connect to Mochi - Type: {Type}, Address: {Address}, Port: {Port}",
            connectionType, address, port);

        await _connection.ConnectAsync(address, port);

        _logger.LogInformation("Successfully connected to Mochi device");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mochi Companion Service stopping...");

        await _monitoringService.StopAsync(cancellationToken);
        _connection.Disconnect();

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("Mochi Companion Service stopped");
    }
}
