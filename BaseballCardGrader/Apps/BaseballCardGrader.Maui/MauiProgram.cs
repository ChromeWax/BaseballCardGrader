using BaseballCardGrader.Maui.Services.Bluetooth;
using BaseballCardGrader.Maui.State;
using BaseballCardGrader.Maui.Views;
using CommunityToolkit.Maui;
using ImageProcessor.DependencyInjection;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

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
            }).UseMauiCommunityToolkitCamera().UseMauiCommunityToolkit();
            builder.Services.AddMauiBlazorWebView();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            builder.Services.AddImageProcessor();
            builder.Services.AddSingleton<ApplicationState>();
            builder.Services.AddSingleton<IEsp32BluetoothService, Esp32BluetoothService>();
            builder.Services.AddTransient<CaptureImagePage>();
            return builder.Build();
        }
    }
}