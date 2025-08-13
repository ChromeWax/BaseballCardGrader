using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions;
using Plugin.BLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseballCardGrader.Maui.Services
{
    public class Esp32BluetoothService : IDisposable
    {
        public event Action<ConnectionState>? OnConnectionStateChanged;
        public event Action<string?>? OnError;
        public event Action<NotificationType>? OnNotification;

        private IBluetoothLE ble;
        private IAdapter adapter;
        private IDevice? connectedDevice;
        private IService? connectedService;
        private ICharacteristic? connectedCharacteristic;

        private const string ServiceUuid = "7123acc7-b24d-4eee-9c7f-ee6302637aef";
        private const string CharacteristicUuid = "8be0f272-b3be-4351-a3fc-d57341aa628e";

        public enum ConnectionState
        {
            Disconnected,
            Scanning,
            Connected
        }

        public enum NotificationType
        {
            LedOn,
            LedOff
        }

        public Esp32BluetoothService()
        {
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.DeviceDiscovered += OnDeviceDiscovered;
        }

        public async Task<bool> EnsurePermissionsAsync()
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

        public bool EnsureBluetoothOn()
        {
            switch (ble.State)
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
                    OnError?.Invoke(null);
                    return true;
            }
        }

        public async Task ConnectAsync()
        {
            if (!await EnsurePermissionsAsync()) return;
            if (!EnsureBluetoothOn()) return;

            OnConnectionStateChanged?.Invoke(ConnectionState.Scanning);

            var filter = new ScanFilterOptions
            {
                ServiceUuids = [new Guid(ServiceUuid)]
            };

            await adapter.StartScanningForDevicesAsync(filter);

            if (adapter.ConnectedDevices.Count == 0)
            {
                OnConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
                OnError?.Invoke("No Esp32 found. Make sure it’s powered and in range.");
            }
        }

        private async void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
        {
            try
            {
                await adapter.ConnectToDeviceAsync(e.Device);
                await adapter.StopScanningForDevicesAsync();
                connectedDevice = e.Device;

                connectedService = await connectedDevice.GetServiceAsync(new Guid(ServiceUuid));
                if (connectedService == null)
                {
                    OnError?.Invoke("Service not found.");
                    return;
                }

                connectedCharacteristic = await connectedService.GetCharacteristicAsync(new Guid(CharacteristicUuid));
                if (connectedCharacteristic == null)
                {
                    OnError?.Invoke("Characteristic not found.");
                    return;
                }

                if (connectedCharacteristic.CanUpdate)
                {
                    connectedCharacteristic.ValueUpdated += OnCharacteristicValueChanged;
                    await connectedCharacteristic.StartUpdatesAsync();
                }

                OnConnectionStateChanged?.Invoke(ConnectionState.Connected);
            }
            catch (DeviceConnectionException)
            {
                await adapter.StopScanningForDevicesAsync();
                OnConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
                OnError?.Invoke("Esp32 found but could not connect.");
            }
        }

        private void OnCharacteristicValueChanged(object sender, CharacteristicUpdatedEventArgs e)
        {
            var value = Encoding.UTF8.GetString(e.Characteristic.Value);
            if (Enum.TryParse(value, out NotificationType note))
                OnNotification?.Invoke(note);
        }

        public async Task SendCommandAsync(string command)
        {
            if (connectedCharacteristic == null) return;
            await connectedCharacteristic.WriteAsync(Encoding.UTF8.GetBytes(command));
        }

        public async Task DisconnectAsync()
        {
            if (connectedDevice == null) return;

            if (connectedCharacteristic != null)
            {
                connectedCharacteristic.ValueUpdated -= OnCharacteristicValueChanged;
                await connectedCharacteristic.StopUpdatesAsync();
            }

            await adapter.DisconnectDeviceAsync(connectedDevice);

            connectedDevice = null;
            connectedService = null;
            connectedCharacteristic = null;

            OnConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
            OnError?.Invoke(null);
        }

        public void Dispose()
        {
            adapter.DeviceDiscovered -= OnDeviceDiscovered;
            if (connectedCharacteristic != null)
                connectedCharacteristic.ValueUpdated -= OnCharacteristicValueChanged;
        }
    }
}
