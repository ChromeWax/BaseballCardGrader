using BaseballCardGrader.Maui.Services;
using BaseballCardGrader.Maui.State;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using ImageProcessor.DependencyInjection;

namespace BaseballCardGrader.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .UseMauiCommunityToolkitCamera();
                

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddImageProcessor();
            builder.Services.AddSingleton<IImageConversionService, ImageConversionService>();
            builder.Services.AddSingleton<ApplicationState>();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}