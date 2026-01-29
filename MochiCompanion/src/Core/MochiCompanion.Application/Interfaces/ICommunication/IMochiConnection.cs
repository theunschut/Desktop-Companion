namespace MochiCompanion.Application.Interfaces.ICommunication;

/// <summary>
/// Represents a connection to the Mochi device (Serial or HTTP).
/// </summary>
public interface IMochiConnection
{
    /// <summary>
    /// Connects to the Mochi device.
    /// </summary>
    /// <param name="address">Connection address (COM port or IP address)</param>
    /// <param name="baudRateOrPort">Baud rate for Serial, port for HTTP</param>
    Task ConnectAsync(string address, int baudRateOrPort);

    /// <summary>
    /// Disconnects from the Mochi device.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Sends a command to the Mochi device.
    /// </summary>
    /// <param name="command">Command string to send</param>
    Task SendCommandAsync(string command);

    /// <summary>
    /// Gets whether the connection is currently active.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Event raised when data is received from the Mochi device.
    /// </summary>
    event EventHandler<string>? DataReceived;
}
