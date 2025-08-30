using System.Text;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

namespace BaseballCardGrader.Maui.Services.Bluetooth;

public class Esp32BluetoothService : IEsp32BluetoothService
{
    public BluetoothConnectionState ConnectionState { get; private set; }
    
    public event Action<BluetoothConnectionState>? OnConnectionStateChanged;
    public event Action<BluetoothNotificationType>? OnNotification;
    public event Action<string>? OnError;

    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
        
    private IDevice? _connectedDevice;
    private IService? _connectedService;
    private ICharacteristic? _connectedCharacteristic;

    private const string ServiceUuid = "7123acc7-b24d-4eee-9c7f-ee6302637aef";
    private const string CharacteristicUuid = "8be0f272-b3be-4351-a3fc-d57341aa628e";

    public Esp32BluetoothService()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
    }

    private async Task<bool> EnsurePermissionsAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Bluetooth>();
            if (status == PermissionStatus.Denied)
            {
                OnError?.Invoke("Bluetooth permission denied. Please enable it.");
                return false;
            }
        }
        return true;
    }

    private bool EnsureBluetoothOn()
    {
        switch (_ble.State)
        {
            case BluetoothState.Unknown:
                OnError?.Invoke("Bluetooth state unknown.");
                return false;
            case BluetoothState.Unavailable:
                OnError?.Invoke("Bluetooth unavailable on this device.");
                return false;
            case BluetoothState.Off:
                OnError?.Invoke("Bluetooth is off. Please enable it.");
                return false;
            default:
                OnError?.Invoke(string.Empty);
                return true;
        }
    }

    public async Task ConnectAsync()
    {
        if (!await EnsurePermissionsAsync()) return;
        if (!EnsureBluetoothOn()) return;

        ConnectionStateHasChanged(BluetoothConnectionState.Scanning);

        var filter = new ScanFilterOptions
        {
            ServiceUuids = [new Guid(ServiceUuid)]
        };

        await _adapter.StartScanningForDevicesAsync(filter);

        if (_adapter.ConnectedDevices.Count == 0)
        {
            ConnectionStateHasChanged(BluetoothConnectionState.Disconnected);
            OnError?.Invoke("No Esp32 found. Make sure it’s powered and in range.");
        }
    }

    private async void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        try
        {
            await _adapter.ConnectToDeviceAsync(e.Device);
            await _adapter.StopScanningForDevicesAsync();
            _connectedDevice = e.Device;

            _connectedService = await _connectedDevice.GetServiceAsync(new Guid(ServiceUuid));
            if (_connectedService == null)
            {
                OnError?.Invoke("Service not found.");
                return;
            }

            _connectedCharacteristic = await _connectedService.GetCharacteristicAsync(new Guid(CharacteristicUuid));
            if (_connectedCharacteristic == null)
            {
                OnError?.Invoke("Characteristic not found.");
                return;
            }

            if (_connectedCharacteristic.CanUpdate)
            {
                _connectedCharacteristic.ValueUpdated += OnCharacteristicValueChanged;
                await _connectedCharacteristic.StartUpdatesAsync();
            }

            ConnectionStateHasChanged(BluetoothConnectionState.Connected);
        }
        catch (DeviceConnectionException)
        {
            await _adapter.StopScanningForDevicesAsync();
            ConnectionStateHasChanged(BluetoothConnectionState.Disconnected);
            OnError?.Invoke("Esp32 found but could not connect.");
        }
    }

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        if (_connectedDevice != null && e.Device.Id == _connectedDevice.Id)
        {
            _connectedDevice = null;
            _connectedService = null;
            _connectedCharacteristic = null;
            ConnectionStateHasChanged(BluetoothConnectionState.Disconnected);
            OnError?.Invoke("ESP32 disconnected or turned off.");
        }
    }
    
    private void OnCharacteristicValueChanged(object? sender, CharacteristicUpdatedEventArgs e)
    {
        var value = Encoding.UTF8.GetString(e.Characteristic.Value);
        if (Enum.TryParse(value, out BluetoothNotificationType note))
            OnNotification?.Invoke(note);
    }

    public async Task SendCommandToEsp32(BluetoothCommand command)
    {
        if (_connectedCharacteristic == null) return;
        await _connectedCharacteristic.WriteAsync(Encoding.UTF8.GetBytes(command.ToString()));
    }

    public async Task DisconnectAsync()
    {
        if (_connectedDevice == null) return;

        if (_connectedCharacteristic != null)
        {
            _connectedCharacteristic.ValueUpdated -= OnCharacteristicValueChanged;
            await _connectedCharacteristic.StopUpdatesAsync();
        }

        await _adapter.DisconnectDeviceAsync(_connectedDevice);

        _connectedDevice = null;
        _connectedService = null;
        _connectedCharacteristic = null;

        ConnectionStateHasChanged(BluetoothConnectionState.Disconnected);
        OnError?.Invoke(string.Empty);
    }

    public void Dispose()
    {
        _adapter.DeviceDiscovered -= OnDeviceDiscovered;
        _adapter.DeviceDisconnected -= OnDeviceDisconnected;
        if (_connectedCharacteristic != null)
            _connectedCharacteristic.ValueUpdated -= OnCharacteristicValueChanged;
    }
    
    private void ConnectionStateHasChanged(BluetoothConnectionState state)
    {
        ConnectionState = state;
        OnConnectionStateChanged?.Invoke(ConnectionState);
    }
}