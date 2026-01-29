using MochiCompanion.Application.Interfaces.ICommunication;

namespace MochiCompanion.IntegrationTests.TestHelpers;

/// <summary>
/// Mock Mochi connection for integration testing.
/// Captures commands sent and simulates responses.
/// </summary>
public class MockMochiConnection : IMochiConnection
{
    private readonly List<string> _sentCommands = new();
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public IReadOnlyList<string> SentCommands => _sentCommands.AsReadOnly();

    public event EventHandler<string>? DataReceived;

    public Task ConnectAsync(string address, int baudRateOrPort)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        _isConnected = false;
    }

    public Task SendCommandAsync(string command)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        _sentCommands.Add(command);
        return Task.CompletedTask;
    }

    public void SimulateResponse(string response)
    {
        DataReceived?.Invoke(this, response);
    }

    public void ClearCommands()
    {
        _sentCommands.Clear();
    }
}
