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
        { ImagePosition.Top, BluetoothCommand.UpOn },
        { ImagePosition.Bottom, BluetoothCommand.DownOn },
        { ImagePosition.Left, BluetoothCommand.LeftOn },
        { ImagePosition.Right, BluetoothCommand.RightOn }
    };
    
    private TaskCompletionSource<BluetoothNotificationType>? _notificationTcs;
    
    public CaptureImagePage(ApplicationState applicationState, NavigationManager navigationManager, IEsp32BluetoothService esp32BluetoothService)
    {
        InitializeComponent();
        _applicationState = applicationState;
        _navigationManager = navigationManager;
        _esp32BluetoothService = esp32BluetoothService;
        
        ClearStoredBitmaps();

        if (_esp32BluetoothService.ConnectionState != BluetoothConnectionState.Connected)
        {
            NavigateOutOfPage(PipelineStep.ConnectToEsp32);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _esp32BluetoothService.OnNotification += OnNotification;
        _esp32BluetoothService.OnConnectionStateChanged += OnConnectionStateChanged;
        
        await SelectDefaultCamera();
        await TurnOnAllLights();
    }
    
    protected override void OnDisappearing()
    {
        _esp32BluetoothService.OnNotification -= OnNotification;
        _esp32BluetoothService.OnConnectionStateChanged -= OnConnectionStateChanged;
        base.OnDisappearing();
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
        var rotatedSkBitmap = ImageConversion.RotateClockwise(skBitmap);
        skBitmap.Dispose();
        _applicationState.ImagePositionToSkBitmap[ImagePosition.All] = rotatedSkBitmap;
        
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
        
        // Captures the image
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        var captureImageCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var stream = await Camera.CaptureImage(captureImageCts.Token);
        
        // Saves image to the application state
        var skBitmap = SKBitmap.Decode(stream);
        var rotatedSkBitmap = ImageConversion.RotateClockwise(skBitmap);
        skBitmap.Dispose();
        _applicationState.ImagePositionToSkBitmap[imagePosition] = rotatedSkBitmap;
        
        // Start listening for led off notification
        _notificationTcs = new TaskCompletionSource<BluetoothNotificationType>();
        
        // Tells the ESP32 to turn on the led 
        await _esp32BluetoothService.SendCommandToEsp32(BluetoothCommand.None);

        // Waits for the ESP32 to confirm the led has turned off
        await _notificationTcs.Task;
    }
    
    private void ClearStoredBitmaps()
    {
        foreach (var skBitmap in _applicationState.ImagePositionToSkBitmap.Values)
        {
            skBitmap.Dispose();
        }
        _applicationState.ImagePositionToSkBitmap.Clear();
    }
    
    private void NavigateOutOfPage(PipelineStep step)
    {
        _applicationState.PipelineStep = step;
        _navigationManager.NavigateTo("/", true);
        Navigation.PopAsync();
    }
}
