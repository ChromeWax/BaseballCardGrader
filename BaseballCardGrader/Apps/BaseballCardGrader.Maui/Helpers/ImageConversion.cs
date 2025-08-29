using SkiaSharp;

namespace BaseballCardGrader.Maui.Helpers;

public static class ImageConversion
{
    public static string ConvertImageToBase64(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        using var ms = new MemoryStream();
        data.SaveTo(ms);
        return Convert.ToBase64String(ms.ToArray());
    }
    
    public static SKBitmap RotateClockwise(SKBitmap bitmap)
    {
        var rotated = new SKBitmap(bitmap.Height, bitmap.Width);
        using var canvas = new SKCanvas(rotated);
        canvas.Translate(rotated.Width / 2, rotated.Height / 2);
        canvas.RotateDegrees(90);
        canvas.Translate(-bitmap.Width / 2, -bitmap.Height / 2);
        canvas.DrawBitmap(bitmap, 0, 0);
        return rotated;
    }
}