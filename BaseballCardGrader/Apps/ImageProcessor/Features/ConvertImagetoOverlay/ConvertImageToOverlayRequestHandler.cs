using Mediator;
using OpenCvSharp;
using System.Threading;
using System.Threading.Tasks;

namespace ImageProcessor.Features.ConvertImageToOverlay;

public class ConvertImageToOverlayRequestHandler : IRequestHandler<ConvertImageToOverlayRequest, string>
{
    public Task<string> Handle(ConvertImageToOverlayRequest request, CancellationToken cancellationToken)
    {
        // Load images from file paths in grayscale
        var topGray = Cv2.ImRead(request.originalTopImagePath, ImreadModes.Grayscale);
        var bottomGray = Cv2.ImRead(request.originalBottomImagePath, ImreadModes.Grayscale);
        var leftGray = Cv2.ImRead(request.originalLeftImagePath, ImreadModes.Grayscale);
        var rightGray = Cv2.ImRead(request.originalRightImagePath, ImreadModes.Grayscale);

        // Validate dimensions
        int width = topGray.Width;
        int height = topGray.Height;

        if (bottomGray.Width != width || bottomGray.Height != height ||
            leftGray.Width != width || leftGray.Height != height ||
            rightGray.Width != width || rightGray.Height != height)
        {
            throw new ArgumentException("All input images must have the same dimensions.");
        }

        // Create a blue channel (all zeros)
        var blue = new Mat(height, width, MatType.CV_8UC1, Scalar.All(0));

        // Create colored versions of grayscale inputs
        var aboveLeft = new Mat();
        Cv2.Merge(new[] { blue, topGray, leftGray }, aboveLeft);  // Blue, Green, Red

        var bottomRight = new Mat();
        Cv2.Merge(new[] { blue, bottomGray, rightGray }, bottomRight);  // Blue, Green, Red

        // Blend the two colored images
        var blended = new Mat();
        Cv2.AddWeighted(aboveLeft, 0.5, bottomRight, 0.5, 0, blended);

        // Save the result as PNG
        Cv2.ImWrite(request.outputFilePath, blended);

        // Return the saved file path
        return Task.FromResult(request.outputFilePath);
    }
}