using SkiaSharp;

namespace BaseballCardGrader.Maui.Helpers;

/// <summary>
/// Helper class for image conversion and manipulation.
/// </summary>
public static class ImageConversion
{
    /// <summary>
    /// Converts an SKBitmap to a Base64 string.
    /// Useful for embedding images in HTML or JSON.
    /// </summary>
    /// <param name="bitmap"><see cref="SKBitmap"/> to be converted.</param>
    /// <returns>Image in base64 string form.</returns>
    public static string ConvertImageToBase64(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        using var ms = new MemoryStream();
        data.SaveTo(ms);
        return Convert.ToBase64String(ms.ToArray());
    }
    
    /// <summary>
    /// Rotates an SKBitmap 90 degrees clockwise.
    /// </summary>
    /// <param name="bitmap"><see cref="SKBitmap"/> to be transformed.</param>
    /// <returns>Transformed <see cref="SKBitmap"/>.</returns>
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