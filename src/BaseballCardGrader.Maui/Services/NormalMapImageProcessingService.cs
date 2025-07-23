using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BaseballCardGrader.Maui.Services;

public class NormalMapImageProcessingService : ImageProcessingService
{
    public override async Task<Stream> GenerateImage(Dictionary<string, IBrowserFile> imageFiles)
    {
        var topTask = await LoadGrayImage(imageFiles["top"]);
        var bottomTask = await LoadGrayImage(imageFiles["bottom"]);
        var leftTask = await LoadGrayImage(imageFiles["left"]);
        var rightTask = await LoadGrayImage(imageFiles["right"]);
        
        var pictures = new[]{topTask, bottomTask, leftTask, rightTask};
        var width = pictures[0].Width;
        var height = pictures[0].Height;

        using Image<Rgb24> aboveLeft = new Image<Rgb24>(width, height);
        using Image<Rgb24> bottomRight = new Image<Rgb24>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte normTop = NormalizePixel(pictures[0][x, y].PackedValue, 0, 255, 0, 127);
                byte normLeft = NormalizePixel(pictures[2][x, y].PackedValue, 0, 255, 0, 127);
                aboveLeft[x, y] = new Rgb24(normTop, normLeft, 0);

                byte normBottom = NormalizePixel(pictures[1][x, y].PackedValue, 0, 255, 128, 255);
                byte normRight = NormalizePixel(pictures[3][x, y].PackedValue, 0, 255, 128, 255);
                bottomRight[x, y] = new Rgb24(normBottom, normRight, 0);
            }
        }

        Image<Rgb24> overlay = new Image<Rgb24>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var a = aboveLeft[x, y];
                var b = bottomRight[x, y];

                overlay[x, y] = new Rgb24(
                    (byte)((a.B + b.B) / 2),
                    (byte)((a.G + b.G) / 2),
                    (byte)((a.R + b.R) / 2)
                );
            }
        }

        var memoryStream = new MemoryStream();
        await overlay.SaveAsJpegAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    byte NormalizePixel(byte value, int minSrc, int maxSrc, int minDst, int maxDst)
    {
        return (byte)((value - minSrc) * (maxDst - minDst) / (float)(maxSrc - minSrc) + minDst);
    }
    public override Task<Stream> GenerateImageFromBytesAsync(Dictionary<string, byte[]> imageBytes)
    {
        Console.WriteLine("This normal map thing got hit");
        throw new NotImplementedException();
    }
}