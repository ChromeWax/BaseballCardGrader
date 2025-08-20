namespace BaseballCardGrader.Maui.Services.Bluetooth;

public interface IEsp32BluetoothService : IDisposable
{
        event Action<BluetoothConnectionState>? OnConnectionStateChanged;
        event Action<BluetoothNotificationType>? OnNotification;
        event Action<string>? OnError;

        Task ConnectAsync();
        Task SendCommandToEsp32(string command);
        Task DisconnectAsync();
}