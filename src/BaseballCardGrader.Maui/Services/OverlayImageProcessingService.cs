using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BaseballCardGrader.Maui.Services;

public class OverlayImageProcessingService : IImageProcessingService
{
    public override async Task<Stream> GenerateImage(Dictionary<string, IBrowserFile> imageFiles)
    {
        // Load all images as grayscale Mats
        var topTask = await LoadGrayImage(imageFiles["top"]);
        var bottomTask = await LoadGrayImage(imageFiles["bottom"]);
        var leftTask = await LoadGrayImage(imageFiles["left"]);
        var rightTask = await LoadGrayImage(imageFiles["right"]);

        Image<L8>[] pictures = new[] {topTask, bottomTask, leftTask, rightTask};
        var top = pictures[0];
        var bottom = pictures[1];
        var left = pictures[2];
        var right = pictures[3];

        using var result = CreateBlendedImage(top, bottom, left, right);

        var output = new MemoryStream();
        await result.SaveAsJpegAsync(output);
        output.Position = 0;
        return output;
    }

    public override async Task<Stream> GenerateImageFromBytesAsync(Dictionary<string, byte[]> imageBytes)
    {
        // Load all images as grayscale Mats
        var topTask = await LoadGrayImage(imageBytes["top"]);
        var bottomTask = await LoadGrayImage(imageBytes["bottom"]);
        var leftTask = await LoadGrayImage(imageBytes["left"]);
        var rightTask = await LoadGrayImage(imageBytes["right"]);

        var pictures = new []{topTask, bottomTask, leftTask, rightTask};
        var top = pictures[0];
        var bottom = pictures[1];
        var left = pictures[2];
        var right = pictures[3];

        using var result = CreateBlendedImage(top, bottom, left, right);

        var output = new MemoryStream();
        await result.SaveAsJpegAsync(output);
        output.Position = 0;
        return output;
    }

    

    private Image<Rgb24> CreateBlendedImage(Image<L8> top, Image<L8> bottom, Image<L8> left, Image<L8> right)
    {
        int width = top.Width;
        int height = top.Height;

        var aboveLeft = new Image<Rgb24>(width, height);
        var bottomRight = new Image<Rgb24>(width, height);
        var blended = new Image<Rgb24>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte topVal = top[x, y].PackedValue;
                byte bottomVal = bottom[x, y].PackedValue;
                byte leftVal = left[x, y].PackedValue;
                byte rightVal = right[x, y].PackedValue;

                aboveLeft[x, y] = new Rgb24(leftVal, topVal, 0);
                bottomRight[x, y] = new Rgb24(rightVal, bottomVal, 0);

                blended[x, y] = new Rgb24(
                    (byte)((aboveLeft[x, y].R + bottomRight[x, y].R) / 2),
                    (byte)((aboveLeft[x, y].G + bottomRight[x, y].G) / 2),
                    (byte)((aboveLeft[x, y].B + bottomRight[x, y].B) / 2));
            }
        }

        return blended;
    }


}

