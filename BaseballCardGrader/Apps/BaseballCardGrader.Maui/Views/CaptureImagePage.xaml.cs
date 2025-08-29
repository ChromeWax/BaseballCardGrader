using BaseballCardGrader.Maui.Helpers;
using BaseballCardGrader.Maui.Services.Bluetooth;
using BaseballCardGrader.Maui.State;
using CommunityToolkit.Maui.Core;
using ImageProcessor.Helper.ImageEffects;
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
        { ImagePosition.Top, BluetoothCommand.UpPulse },
        { ImagePosition.Bottom, BluetoothCommand.DownPulse },
        { ImagePosition.Left, BluetoothCommand.LeftPulse },
        { ImagePosition.Right, BluetoothCommand.RightPulse }
    };
    
    private TaskCompletionSource<BluetoothNotificationType>? _notificationTcs;
    
    public CaptureImagePage(ApplicationState applicationState, NavigationManager navigationManager, IEsp32BluetoothService esp32BluetoothService)
    {
        InitializeComponent();
        _applicationState = applicationState;
        _navigationManager = navigationManager;
        _esp32BluetoothService = esp32BluetoothService;

        if (_esp32BluetoothService.ConnectionState != BluetoothConnectionState.Connected)
        {
            NavigateOutOfPage(PipelineStep.ConnectToEsp32);
            return;
        }
        
        _esp32BluetoothService.OnNotification += OnNotification;
        _esp32BluetoothService.OnConnectionStateChanged += OnConnectionStateChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        await SelectDefaultCamera();

        await TurnOnAllLights();
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
        _esp32BluetoothService.OnConnectionStateChanged -= OnConnectionStateChanged;
    }
    
    private void OnNotification(BluetoothNotificationType type)
    {
        _notificationTcs?.SetResult(type);
    }
    
    private async void OnTakeFourPhotosClicked(object sender, EventArgs e)
    {
        PipelineStep step = PipelineStep.CaptureImages;
        try
        {
            // Disable the button to prevent clicks during capture
            ((Button)sender).IsEnabled = false;

            await TakePhotoOfAllLightsAndStore();

            await TakePhotoOfSingleImagePositionAndStore(ImagePosition.Top);
            await TakePhotoOfSingleImagePositionAndStore(ImagePosition.Right);
            await TakePhotoOfSingleImagePositionAndStore(ImagePosition.Bottom);
            await TakePhotoOfSingleImagePositionAndStore(ImagePosition.Left);

            step = PipelineStep.ProcessImages;
        }
        catch (Exception ex)
        {
            await _esp32BluetoothService.DisconnectAsync();
            await DisplayAlert("Error", "Something went wrong, please reconnect", "OK");
            
            step = PipelineStep.ConnectToEsp32;
        }
        finally
        {
            NavigateOutOfPage(step);
        }
    }
    
    private void OnConnectionStateChanged(BluetoothConnectionState state)
    {
        if (state != BluetoothConnectionState.Disconnected) return;
        NavigateOutOfPage(PipelineStep.ConnectToEsp32);
    }

    private async Task TurnOnAllLights()
    {
        _notificationTcs = new TaskCompletionSource<BluetoothNotificationType>();
        await _esp32BluetoothService.SendCommandToEsp32(BluetoothCommand.ToggleAllOn);
        await _notificationTcs.Task;
    }
    
    private async Task TurnOffAllLights()
    {
        _notificationTcs = new TaskCompletionSource<BluetoothNotificationType>();
        await _esp32BluetoothService.SendCommandToEsp32(BluetoothCommand.None);
        await _notificationTcs.Task;
    }

    private async Task TakePhotoOfAllLightsAndStore()
    {
        // Captures the image
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        var captureImageCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var stream = await Camera.CaptureImage(captureImageCts.Token);
        
        // Saves image to the application state
        var skBitmap = SKBitmap.Decode(stream);
        _applicationState.ImagePositionToSkBitmap[ImagePosition.All] = skBitmap;
        
        // Turn all leds off
        await TurnOffAllLights();
    }

    private async Task TakePhotoOfSingleImagePositionAndStore(ImagePosition imagePosition)
    {
        if (imagePosition == ImagePosition.All) return;
        
        // Start listening for led on notification
        _notificationTcs = new TaskCompletionSource<BluetoothNotificationType>();
        
        // Tells the ESP32 to turn on the led 
        await _esp32BluetoothService.SendCommandToEsp32(_imagePositionToCommand[imagePosition]);
        
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
        skBitmap = ImageConversion.RotateClockwise(skBitmap);
        _applicationState.ImagePositionToSkBitmap[imagePosition] = skBitmap;

        // Waits for the ESP32 to confirm the led has turned off
        await _notificationTcs.Task;
    }
    
    private void NavigateOutOfPage(PipelineStep step)
    {
        _applicationState.PipelineStep = step;
        _navigationManager.NavigateTo("/", true);
        Navigation.PopAsync();
    }
}
