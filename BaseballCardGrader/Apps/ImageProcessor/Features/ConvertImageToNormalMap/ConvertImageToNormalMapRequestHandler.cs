using System.Numerics;
using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessor.Features.ConvertImageToNormalMap;

public class ConvertImageToNormalMapRequestHandler : IRequestHandler<ConvertImageToNormalMapRequest, Image<Rgb24>>
{
    // TODO: Need to fix Normal Map logic
    public Task<Image<Rgb24>> Handle(ConvertImageToNormalMapRequest request, CancellationToken cancellationToken)
    {
        using var topImage = Image.Load<L8>(request.originalTopImagePath);
        using var bottomImage = Image.Load<L8>(request.originalBottomImagePath);
        using var rightImage = Image.Load<L8>(request.originalRightImagePath);
        using var leftImage = Image.Load<L8>(request.originalLeftImagePath);

        int width = topImage.Width;
        int height = topImage.Height;
        
        using Image<Rgb24> aboveLeft = new Image<Rgb24>(width, height);
        using Image<Rgb24> bottomRight = new Image<Rgb24>(width, height);
        
        // for (int y = 0; y < height; y++)
        // {
        //     for (int x = 0; x < width; x++)
        //     {
        //         byte normTop = NormalizePixel(topImage[x, y].PackedValue, 0, 255, 0, 127);
        //         byte normLeft = NormalizePixel(leftImage[x, y].PackedValue, 0, 255, 0, 127);
        //         aboveLeft[x, y] = new Rgb24(normTop, normLeft, 0);
        //
        //         byte normBottom = NormalizePixel(bottomImage[x, y].PackedValue, 0, 255, 128, 255);
        //         byte normRight = NormalizePixel(rightImage[x, y].PackedValue, 0, 255, 128, 255);
        //         bottomRight[x, y] = new Rgb24(normBottom, normRight, 0);
        //     }
        // }
        //
        // Image<Rgb24> overlay = new Image<Rgb24>(width, height);
        // for (int y = 0; y < height; y++)
        // {
        //     for (int x = 0; x < width; x++)
        //     {
        //         var a = aboveLeft[x, y];
        //         var b = bottomRight[x, y];
        //
        //         overlay[x, y] = new Rgb24(
        //             (byte)((a.B + b.B) / 2),
        //             (byte)((a.G + b.G) / 2),
        //             (byte)((a.R + b.R) / 2)
        //         );
        //     }
        // }

        // return Task.FromResult(overlay);
        return Task.FromResult(aboveLeft);
    }
    
    private byte NormalizePixel(byte value, int minSrc, int maxSrc, int minDst, int maxDst)
    {
        return (byte)((value - minSrc) * (maxDst - minDst) / (float)(maxSrc - minSrc) + minDst);
    }
}

