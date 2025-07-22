using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace BaseballCardGrader.Maui.Services;

public abstract class IImageProcessingService
{
    public abstract Task<Stream> GenerateImage(Dictionary<string, IBrowserFile> imageFiles);
    public async Task<Image<L8>> LoadGrayImage(IBrowserFile file)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 10_000_000);
        var image = await Image.LoadAsync<L8>(stream);
        return image;
    }

    public async Task<Image<L8>> LoadGrayImage(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        var image = await Image.LoadAsync<L8>(stream); // L8 = grayscale
        return image;
    }

    public abstract Task<Stream> GenerateImageFromBytesAsync(Dictionary<string, byte[]> imageBytes);
}