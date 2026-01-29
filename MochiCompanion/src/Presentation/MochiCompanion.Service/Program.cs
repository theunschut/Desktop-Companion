using MochiCompanion.Application.Interfaces.ICommunication;
using MochiCompanion.Application.Interfaces.IMonitors;
using MochiCompanion.Application.Interfaces.IServices;
using MochiCompanion.Application.Services;
using MochiCompanion.Infrastructure.Communication;
using MochiCompanion.Infrastructure.Monitors;
using MochiCompanion.Service;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/mochi-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Mochi Companion Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Mochi Companion Service";
    });

    // Add Serilog
    builder.Services.AddSerilog();

    // Register HttpClient for WiFi connection
    builder.Services.AddHttpClient<HttpConnection>();

    // Register Communication
    builder.Services.AddSingleton<ICommandBuilder, CommandBuilder>();
    builder.Services.AddSingleton<SerialConnection>();
    builder.Services.AddSingleton<HttpConnection>();

    // Register the connection (default to Serial, can be configured)
    builder.Services.AddSingleton<IMochiConnection>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var connectionType = config.GetValue<string>("Mochi:ConnectionType", "Serial");

        return connectionType.ToLower() switch
        {
            "http" => sp.GetRequiredService<HttpConnection>(),
            _ => sp.GetRequiredService<SerialConnection>()
        };
    });

    // Register Services
    builder.Services.AddSingleton<IMoodService, MoodService>();
    builder.Services.AddSingleton<IMonitoringService, MonitoringService>();

    // Register Monitors
    builder.Services.AddSingleton<ISystemMonitor, TimeMonitor>();
    builder.Services.AddSingleton<ISystemMonitor, AudioMonitor>();
    builder.Services.AddSingleton<ISystemMonitor, SystemMonitor>();
    builder.Services.AddSingleton<ISystemMonitor, ApplicationMonitor>();

    // Register Worker
    builder.Services.AddHostedService<MochiWorker>();

    var host = builder.Build();

    // Register all monitors with MonitoringService
    var monitoringService = host.Services.GetRequiredService<IMonitoringService>();
    var monitors = host.Services.GetServices<ISystemMonitor>();
    foreach (var monitor in monitors)
    {
        monitoringService.RegisterMonitor(monitor);
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
