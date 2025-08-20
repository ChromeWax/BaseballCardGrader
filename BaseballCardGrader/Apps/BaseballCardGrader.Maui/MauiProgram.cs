using BaseballCardGrader.Maui.Services.Bluetooth;
using BaseballCardGrader.Maui.Services.ImageConversion;
using BaseballCardGrader.Maui.State;
using BaseballCardGrader.Maui.Views;
using CommunityToolkit.Maui;
using ImageProcessor.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BaseballCardGrader.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            }).UseMauiCommunityToolkitCamera();
            builder.Services.AddMauiBlazorWebView();
            
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            builder.Services.AddImageProcessor();
            builder.Services.AddSingleton<ApplicationState>();
            builder.Services.AddSingleton<IEsp32BluetoothService, Esp32BluetoothService>();
            builder.Services.AddSingleton<IImageConversionService, ImageConversionService>();

            builder.Services.AddTransient<CaptureImagePage>();
            
            return builder.Build();
        }
    }
}