using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BaseballCardGrader.Maui.Services.ImageConversion;

public class ImageConversionService :  IImageConversionService 
{
    public Image<Rgb24> ConvertJpegBytesToRgbImage(byte[] jpegBytes)
    {
        return SixLabors.ImageSharp.Image.Load<Rgb24>(jpegBytes);
    }

    public Image<L8> ConvertJpegBytesToGrayscaleImage(byte[] jpegBytes)
    {
        return SixLabors.ImageSharp.Image.Load<L8>(jpegBytes);
    }

    public async Task<string> ConvertRgbImageToBase64(Image<Rgb24> image)
    {
        using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);
        ms.Position = 0;
        var buffer = ms.ToArray();
        return Convert.ToBase64String(buffer);
    }
}
