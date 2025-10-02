namespace BaseballCardGrader.Maui.Services.Bluetooth;

/// <summary>
/// Interface for ESP32 Bluetooth Service.
/// </summary>
public interface IEsp32BluetoothService : IDisposable
{
        /// <summary>
        /// Current connection state of the Bluetooth service.
        /// </summary>
        BluetoothConnectionState ConnectionState { get; } 
        
        /// <summary>
        /// Event triggered when the connection state changes.
        /// </summary>
        event Action<BluetoothConnectionState>? OnConnectionStateChanged;
        /// <summary>
        /// Event triggered when a notification is received from the ESP32 device.
        /// </summary>
        event Action<BluetoothNotificationType>? OnNotification;
        /// <summary>
        /// Event triggered when an error occurs.
        /// </summary>
        event Action<string>? OnError;

        /// <summary>
        /// Connects to the ESP32 device asynchronously.
        /// </summary>
        /// <returns>Task that can be awaited.</returns>
        Task ConnectAsync();
        
        /// <summary>
        /// Sends a command to the ESP32 device asynchronously.
        /// </summary>
        /// <param name="command"><see cref="BluetoothCommand"/> that can be sent to ESP32.</param>
        /// <returns>Task that can be awaited.</returns>
        Task SendCommandToEsp32(BluetoothCommand command);
        
        /// <summary>
        /// Disconnects from the ESP32 device asynchronously.
        /// </summary>
        /// <returns>Task that can be awaited.</returns>
        Task DisconnectAsync();
}