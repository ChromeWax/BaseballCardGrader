using BaseballCardGrader.Maui.Services.Bluetooth;
using BaseballCardGrader.Maui.State;
using CommunityToolkit.Maui.Core;
using Microsoft.AspNetCore.Components;
using SkiaSharp;

namespace BaseballCardGrader.Maui.Views;

public partial class CaptureImagePage : ContentPage, IDisposable
{
    private readonly ApplicationState _applicationState;
    private readonly NavigationManager _navigationManager;
    private readonly IEsp32BluetoothService _esp32BluetoothService;
    
    private readonly Dictionary<ImagePosition, BluetoothCommand> _imagePositionToCommand = new()
    {
        { ImagePosition.Top, BluetoothCommand.Up },
        { ImagePosition.Bottom, BluetoothCommand.Down },
        { ImagePosition.Left, BluetoothCommand.Left },
        { ImagePosition.Right, BluetoothCommand.Right }
    };
    
    private TaskCompletionSource<BluetoothNotificationType>? _notificationTcs;
    
    public CaptureImagePage(ApplicationState applicationState, NavigationManager navigationManager, IEsp32BluetoothService esp32BluetoothService)
    {
        InitializeComponent();
        _applicationState = applicationState;
        _navigationManager = navigationManager;
        _esp32BluetoothService = esp32BluetoothService;

        SelectDefaultCamera();
        
        _esp32BluetoothService.OnNotification += OnNotification;
    }

    private async Task SelectDefaultCamera()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cameras = await Camera.GetAvailableCameras(cancellationTokenSource.Token);
        var defaultBackCamera = cameras.FirstOrDefault(c => c.Position == CameraPosition.Rear);

        if (defaultBackCamera != null)
        {
            Camera.SelectedCamera = defaultBackCamera;
        }
    }
    
    private void OnZoomSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        Camera.ZoomFactor = (float)e.NewValue;
    }

    public void Dispose()
    {
        _esp32BluetoothService.OnNotification -= OnNotification;
    }
    
    private void OnNotification(BluetoothNotificationType type)
    {
        _notificationTcs?.SetResult(type);
    }
    
    private async void OnTakeFourPhotosClicked(object sender, EventArgs e)
    {
        try
        {
            // Disable the button to prevent clicks during capture
            ((Button)sender).IsEnabled = false;

            await TakePhotoAndStore(ImagePosition.Top);
            await TakePhotoAndStore(ImagePosition.Right);
            await TakePhotoAndStore(ImagePosition.Bottom);
            await TakePhotoAndStore(ImagePosition.Left);

            _applicationState.PipelineStep = PipelineStep.ProcessImages;
        }
        catch (Exception ex)
        {
            await _esp32BluetoothService.DisconnectAsync();
            await DisplayAlert("Error", "Something went wrong, please reconnect", "OK");
            
            _applicationState.PipelineStep = PipelineStep.ConnectToEsp32;
        }
        finally
        {
            _navigationManager.NavigateTo("/", true);
            await Navigation.PopAsync();
        }
    }

    private async Task TakePhotoAndStore(ImagePosition imagePosition)
    {
        // Start listening for led on notification
        _notificationTcs = new TaskCompletionSource<BluetoothNotificationType>();
        
        // Tells the ESP32 to turn on the led 
        await _esp32BluetoothService.SendCommandToEsp32(_imagePositionToCommand[imagePosition].ToString());
        
        // Wait for the ESP32 to confirm the led has turned on
        await _notificationTcs.Task;
        
        // Start listening for led off notification
        _notificationTcs = new TaskCompletionSource<BluetoothNotificationType>();

        // Captures the image
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        var captureImageCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var stream = await Camera.CaptureImage(captureImageCts.Token);
        
        // Saves image to the application state
        var skBitmap = SKBitmap.Decode(stream);
        _applicationState.ImagePositionToSkBitmap[imagePosition] = skBitmap;

        // Waits for the ESP32 to confirm the led has turned off
        await _notificationTcs.Task;
    }
}
