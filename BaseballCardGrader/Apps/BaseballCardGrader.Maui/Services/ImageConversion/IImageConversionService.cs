using SkiaSharp;

namespace BaseballCardGrader.Maui.Services.ImageConversion;

public interface IImageConversionService
{
    public string ConvertImageToBase64(SKBitmap bitmap);
}