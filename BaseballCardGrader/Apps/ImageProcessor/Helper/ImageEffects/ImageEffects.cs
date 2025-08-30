using SkiaSharp;

namespace ImageProcessor.Helper.ImageEffects;

public static class ImageEffects
{
    #region Create RGB Image from Three Grayscale Images
    /// <summary>
    /// Creates an RGB image from three separate grayscale images representing the red, green, and blue channels.
    /// </summary>
    /// <param name="redChannel"></param>
    /// <param name="greenChannel"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static SKBitmap CreateRedGreenImageFromTwoGrayscaleImages(byte[] redChannel, byte[] greenChannel, int width, int height)
    {
        var bmp = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var pixels = new SKColor[width * height];

        Parallel.For(0, pixels.Length, i =>
        {
            pixels[i] = new SKColor(redChannel[i], greenChannel[i], 0, 255);
        });

        bmp.Pixels = pixels;
        return bmp;
    }
    #endregion

    #region Converts Image to Grayscale Byte Array
    /// <summary>
    /// Converts an SKBitmap to a grayscale byte array using the luminosity method.
    /// </summary>
    /// <param name="bmp"></param>
    /// <returns></returns>
    public static byte[] ToGrayscaleBytes(SKBitmap bmp)
    {
        int width = bmp.Width;
        int height = bmp.Height;
        var gray = new byte[width * height];
        var pixels = bmp.Pixels;

        Parallel.For(0, pixels.Length, i =>
        {
            var c = pixels[i];
            gray[i] = (byte)Math.Clamp((int)Math.Round(0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue), 0, 255);
        });

        return gray;
    }
    #endregion
    
    #region Resize Image
    /// <summary>
    /// Resizes a bitmap to the specified width and height.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static SKBitmap ResizeBitmap(SKBitmap src, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var resized = src.Resize(info, SKFilterQuality.Medium);
        return resized;
    }
    #endregion
    
    #region Fill Color Channels
    public static void FillBlueChannel(SKBitmap bitmap, byte blueValue)
    {
        var pixels = bitmap.Pixels;

        Parallel.For(0, pixels.Length, i =>
        {
            var c = pixels[i];
            pixels[i] = new SKColor(c.Red, c.Green, blueValue, c.Alpha);
        });

        bitmap.Pixels = pixels;
    }
    #endregion

    #region Output Levels
    /// <summary>
    /// Takes an image and applies output levels adjustment.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="outputMin"></param>
    /// <param name="outputMax"></param>
    public static void ApplyOutputLevels(SKBitmap source, int outputMin, int outputMax)
    {
        var pixels = source.Pixels;

        var outputMinNormalized = outputMin / 255f;
        var outputMaxNormalized = outputMax / 255f;
        var scale = outputMaxNormalized - outputMinNormalized;

        Parallel.For(0, pixels.Length, i =>
        {
            var c = pixels[i];

            var r = AdjustChannel(c.Red, outputMinNormalized, scale);
            var g = AdjustChannel(c.Green, outputMinNormalized, scale);
            var b = AdjustChannel(c.Blue, outputMinNormalized, scale);

            pixels[i] = new SKColor(r, g, b, c.Alpha);
        });

        source.Pixels = pixels;
    }

    /// <summary>
    ///  Adjusts a single color channel based on output levels.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="outputMinNormalized"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    private static byte AdjustChannel(byte value, float outputMinNormalized, float scale)
    {
        var channelValueNormalized = value / 255f;
        var adjustedValue = outputMinNormalized + channelValueNormalized * scale;
        return (byte)Math.Clamp((int)Math.Round(adjustedValue * 255), 0, 255);
    }
    #endregion

    #region Overlay Images
    /// <summary>
    /// Overlays one image on top of another with specified alpha transparency.
    /// </summary>
    /// <param name="baseImage"></param>
    /// <param name="overlayImage"></param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    public static SKBitmap OverlayImages(SKBitmap baseImage, SKBitmap overlayImage, float alpha)
    {
        var result = baseImage.Copy();
        using (var canvas = new SKCanvas(result))
        using (var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(alpha * 255)), FilterQuality = SKFilterQuality.Low })
        {
            canvas.DrawBitmap(overlayImage, SKPoint.Empty, paint);
        }

        return result;
    }
    #endregion

    #region Create Overlay Image from Four Images
    /// <summary>
    /// Creates an overlay image from four directional grayscale images (top, right, bottom, left).
    /// </summary>
    /// <param name="topImage"></param>
    /// <param name="rightImage"></param>
    /// <param name="bottomImage"></param>
    /// <param name="leftImage"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<SKBitmap> CreateOverlayImage(SKBitmap topImage, SKBitmap rightImage, SKBitmap bottomImage, SKBitmap leftImage)
    {
        var width = topImage.Width;
        var height = topImage.Height;
        
        if (bottomImage.Width != width || bottomImage.Height != height ||
            leftImage.Width != width || leftImage.Height != height ||
            rightImage.Width != width || rightImage.Height != height)
            throw new ArgumentException("All input images must have the same dimensions.");

        // Run grayscale conversions in parallel
        var topGrayTask = Task.Run(() => ToGrayscaleBytes(topImage));
        var bottomGrayTask = Task.Run(() => ToGrayscaleBytes(bottomImage));
        var rightGrayTask = Task.Run(() => ToGrayscaleBytes(rightImage));
        var leftGrayTask = Task.Run(() => ToGrayscaleBytes(leftImage));

        await Task.WhenAll(topGrayTask, bottomGrayTask, rightGrayTask, leftGrayTask);

        var topGray = topGrayTask.Result;
        var bottomGray = bottomGrayTask.Result;
        var rightGray = rightGrayTask.Result;
        var leftGray = leftGrayTask.Result;

        // Run color image creations in parallel
        var aboveLeftTask = Task.Run(() => CreateRedGreenImageFromTwoGrayscaleImages(leftGray, topGray, width, height));
        var bottomRightTask = Task.Run(() => CreateRedGreenImageFromTwoGrayscaleImages(rightGray, bottomGray, width, height));

        await Task.WhenAll(aboveLeftTask, bottomRightTask);

        var aboveLeft = aboveLeftTask.Result;
        var bottomRight = bottomRightTask.Result;

        var result = OverlayImages(aboveLeft, bottomRight, 0.5f);
        
        aboveLeft.Dispose();
        bottomRight.Dispose();

        return result;
    }
    #endregion
    
    #region Create Normal Image from Four Images
    /// <summary>
    /// Creates an overlay image from four directional grayscale images (top, right, bottom, left).
    /// </summary>
    /// <param name="topImage"></param>
    /// <param name="rightImage"></param>
    /// <param name="bottomImage"></param>
    /// <param name="leftImage"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<SKBitmap> CreateNormalImage(SKBitmap topImage, SKBitmap rightImage, SKBitmap bottomImage, SKBitmap leftImage)
    {
        var width = topImage.Width;
        var height = topImage.Height;
        
        if (bottomImage.Width != width || bottomImage.Height != height ||
            leftImage.Width != width || leftImage.Height != height ||
            rightImage.Width != width || rightImage.Height != height)
            throw new ArgumentException("All input images must have the same dimensions.");

        // Run grayscale conversions in parallel
        var topGrayTask = Task.Run(() => ToGrayscaleBytes(topImage));
        var bottomGrayTask = Task.Run(() => ToGrayscaleBytes(bottomImage));
        var rightGrayTask = Task.Run(() => ToGrayscaleBytes(rightImage));
        var leftGrayTask = Task.Run(() => ToGrayscaleBytes(leftImage));

        await Task.WhenAll(topGrayTask, bottomGrayTask, rightGrayTask, leftGrayTask);

        var topGray = topGrayTask.Result;
        var bottomGray = bottomGrayTask.Result;
        var rightGray = rightGrayTask.Result;
        var leftGray = leftGrayTask.Result;

        // Run color image creations in parallel
        var aboveLeftTask = Task.Run(() => CreateRedGreenImageFromTwoGrayscaleImages(leftGray, topGray, width, height));
        var bottomRightTask = Task.Run(() => CreateRedGreenImageFromTwoGrayscaleImages(rightGray, bottomGray, width, height));

        await Task.WhenAll(aboveLeftTask, bottomRightTask);

        var aboveLeft = aboveLeftTask.Result;
        var bottomRight = bottomRightTask.Result;
        
        ApplyOutputLevels(aboveLeft, 127, 0);
        ApplyOutputLevels(bottomRight, 128, 255);

        var result = OverlayImages(aboveLeft, bottomRight, 0.5f);
        
        FillBlueChannel(result, 255);
        
        aboveLeft.Dispose();
        bottomRight.Dispose();

        return result;
    }
    #endregion
}
