using CardGraderMAUI.Services;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace CardGraderMAUI
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

            builder.Services.AddSingleton<CameraService>();

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<IImageProcessingService, OverlayImageProcessingService>();
            builder.Services.AddSingleton<IImageProcessingService, NormalMapImageProcessingService>();
            builder.Services.AddSingleton<CapturedImageStoreService>();
            builder.Services.AddSingleton<NormalMapImageProcessingService>();
            builder.Services.AddSingleton<OverlayImageProcessingService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}