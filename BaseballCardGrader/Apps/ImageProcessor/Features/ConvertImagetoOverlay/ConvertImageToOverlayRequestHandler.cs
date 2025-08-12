using System.Globalization;
using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessor.Features.ConvertImageToOverlay;

public class ConvertImageToOverlayRequestHandler : IRequestHandler<ConvertImageToOverlayRequest, Image<Rgb24>>
{
    public Task<Image<Rgb24>> Handle(ConvertImageToOverlayRequest request, CancellationToken cancellationToken)
    {
        // Load images from file paths in grayscale
        var topGray = request.originalTopImage;
        var bottomGray = request.originalBottomImage;
        var leftGray = request.originalLeftImage;
        var rightGray = request.originalRightImage;

        // Validate dimensions
        var width = topGray.Width;
        var height = topGray.Height;

        if (bottomGray.Width != width || bottomGray.Height != height ||
            leftGray.Width != width || leftGray.Height != height ||
            rightGray.Width != width || rightGray.Height != height)
            throw new ArgumentException("All input images must have the same dimensions.");
        
// Create the two color images
        var aboveLeft = CreateColorImage(leftGray, topGray, height, width);
        var bottomRight = CreateColorImage(rightGray, bottomGray, height, width);

// Blend them together 50/50
        aboveLeft.Mutate(ctx => ctx.DrawImage(bottomRight, 0.5f));
        // aboveLeft.Save("C:\\Users\\ricky\\Desktop\\BaseballCardGrader\\BaseballCardGrader\\test\\overlay_imagesharp.png");
        return Task.FromResult(aboveLeft);
    }
    
    // Convert grayscale to color with specified channel assignments
    private Image<Rgb24> CreateColorImage(Image<L8> redChannel, Image<L8> greenChannel, int height, int width)
    {
        var colorImage = new Image<Rgb24>(width, height);

        // For ImageSharp 2.x+
        for (int y = 0; y < height; y++)
        {
            var rRow = redChannel.DangerousGetPixelRowMemory(y).Span;
            var gRow = greenChannel.DangerousGetPixelRowMemory(y).Span;
            var colorRow = colorImage.DangerousGetPixelRowMemory(y).Span;

            for (int x = 0; x < width; x++)
            {
                colorRow[x] = new Rgb24(
                    rRow[x].PackedValue,
                    gRow[x].PackedValue,
                    0
                );
            }
        }

        return colorImage;
    }
}
