using BaseballCardGrader.Maui.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace BaseballCardGrader.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                }).UseMauiCommunityToolkitCamera();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();


            builder.Services.AddSingleton<Esp32BluetoothService>();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
