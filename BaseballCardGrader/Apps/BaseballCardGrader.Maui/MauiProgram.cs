using BaseballCardGrader.Maui.Services;
using BaseballCardGrader.Maui.State;
using CommunityToolkit.Maui;
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
            }).UseMauiCommunityToolkitCamera().UseMauiCommunityToolkit();
            builder.Services.AddMauiBlazorWebView();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<ApplicationState>();
            builder.Services.AddSingleton<Esp32BluetoothService>();
            builder.Services.AddSingleton<IImageConversionService, ImageConversionService>();
            return builder.Build();
        }
    }
}