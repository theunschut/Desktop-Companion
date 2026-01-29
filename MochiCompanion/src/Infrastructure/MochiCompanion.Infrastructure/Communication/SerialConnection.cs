using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;
using MochiCompanion.Application.Exceptions;
using MochiCompanion.Application.Interfaces.ICommunication;

namespace MochiCompanion.Infrastructure.Communication;

/// <summary>
/// USB Serial connection to Mochi device.
/// </summary>
public class SerialConnection : IMochiConnection, IDisposable
{
    private readonly ILogger<SerialConnection> _logger;
    private SerialPort? _serialPort;

    public bool IsConnected => _serialPort?.IsOpen ?? false;
    public event EventHandler<string>? DataReceived;

    public SerialConnection(ILogger<SerialConnection> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string portName, int baudRate)
    {
        try
        {
            _serialPort = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                Encoding = Encoding.ASCII
            };

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();

            _logger.LogInformation("Connected to {Port} at {BaudRate} baud", portName, baudRate);

            // Wait for Arduino to initialize (important for ESP32)
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Port}", portName);
            throw new ConnectionException($"Failed to connect to {portName}", portName, ex);
        }
    }

    public void Disconnect()
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.DataReceived -= OnDataReceived;
            _serialPort.Close();
            _logger.LogInformation("Disconnected from serial port");
        }
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
            await _serialPort!.BaseStream.WriteAsync(
                Encoding.ASCII.GetBytes(command + "\n"));

            _logger.LogDebug("Sent: {Command}", command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command: {Command}", command);
            throw new ConnectionException($"Failed to send command: {command}", ex);
        }
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var data = _serialPort!.ReadLine();
            _logger.LogDebug("Received: {Data}", data);
            DataReceived?.Invoke(this, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading serial data");
        }
    }

    public void Dispose()
    {
        Disconnect();
        _serialPort?.Dispose();
    }
}
