using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BaseballCardGrader.Maui.Services.ImageConversion;

public interface IImageConversionService
{
    public Image<Rgb24> ConvertJpegBytesToRgbImage(byte[] jpegBytes);
    
    public Image<L8> ConvertJpegBytesToGrayscaleImage(byte[] jpegBytes);
    
    public Task<string> ConvertRgbImageToBase64(Image<Rgb24> image);
}