using Microsoft.Extensions.Logging;
using MochiCompanion.Application.Exceptions;
using MochiCompanion.Application.Interfaces.ICommunication;

namespace MochiCompanion.Infrastructure.Communication;

/// <summary>
/// HTTP connection to Mochi device over WiFi.
/// </summary>
public class HttpConnection : IMochiConnection, IDisposable
{
    private readonly ILogger<HttpConnection> _logger;
    private readonly HttpClient _httpClient;
    private string _baseUrl = "";
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public event EventHandler<string>? DataReceived;

    public HttpConnection(ILogger<HttpConnection> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task ConnectAsync(string address, int port = 80)
    {
        // Treat address as IP address for HTTP, port parameter as HTTP port        _baseUrl = $"http://{address}:{port}";

        try
        {
            // Test connection
            var response = await _httpClient.GetAsync($"{_baseUrl}/status");
            response.EnsureSuccessStatusCode();

            _isConnected = true;
            _logger.LogInformation("Connected to Mochi via HTTP at {Url}", _baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Url}", _baseUrl);
            _isConnected = false;
            throw new ConnectionException($"Failed to connect to {_baseUrl}", address, ex);
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _logger.LogInformation("Disconnected from HTTP endpoint");
    }

    public async Task SendCommandAsync(string command)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send command - not connected");
            throw new ConnectionException("Not connected to Mochi device");
        }

        try
        {
            // Parse command and convert to HTTP request
            var parts = command.Split(':');
            var endpoint = parts[0].ToLower();

            HttpResponseMessage response = endpoint switch
            {
                "mood" => await _httpClient.PostAsync(
                    $"{_baseUrl}/mood?mood={parts[1]}&priority={parts[2]}" +
                    (parts.Length > 3 ? $"&duration={parts[3]}" : ""), null),

                "pos" => await _httpClient.PostAsync(
                    $"{_baseUrl}/position?position={parts[1]}&priority={parts[2]}", null),

                "anim" => await _httpClient.PostAsync(
                    $"{_baseUrl}/animation?animation={parts[1]}", null),

                "idle" => await _httpClient.PostAsync(
                    $"{_baseUrl}/idle?enabled={parts[1]}", null),

                "blink" => await _httpClient.PostAsync(
                    $"{_baseUrl}/blink?enabled={parts[1]}", null),

                "reset" => await _httpClient.PostAsync($"{_baseUrl}/reset", null),

                _ => throw new ArgumentException($"Unknown command: {endpoint}")
            };

            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Sent HTTP: {Command}", command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HTTP command: {Command}", command);
            throw new ConnectionException($"Failed to send command: {command}", ex);
        }
    }

    public void Dispose()
    {
        Disconnect();
        // Don't dispose HttpClient - it's managed by DI container
    }
}
