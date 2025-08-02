using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace BaseballCardGrader.Maui.Services;

public abstract class ImageProcessingService
{
    public abstract Task<Stream> GenerateImage(Dictionary<string, IBrowserFile> imageFiles);
    public abstract Task<Stream> GenerateImageFromBytesAsync(Dictionary<string, byte[]> imageBytes);

    protected async Task<Image<L8>> LoadGrayImage(IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 10_000_000);
        
        // L8 = grayscale
        var image = await Image.LoadAsync<L8>(stream);
        
        return image;
    }

    protected async Task<Image<L8>> LoadGrayImage(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        
        // L8 = grayscale
        var image = await Image.LoadAsync<L8>(stream); 
        
        return image;
    }
}

public enum ImagePosition
{
    Top,
    Bottom,
    Left,
    Right
}