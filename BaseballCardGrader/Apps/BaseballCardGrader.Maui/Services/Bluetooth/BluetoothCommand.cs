namespace BaseballCardGrader.Maui.Services.Bluetooth;

/// <summary>
/// Commands that can be sent to the ESP32 device via Bluetooth.
/// </summary>
public enum BluetoothCommand
{
    None,
    UpOn,
    DownOn,
    LeftOn,
    RightOn,
    ToggleAllOn
}