using ImageProcessor.Helper.ImageEffects;
using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.ConvertFourImagesToNormalMap;

public class ConvertFourImagesToNormalMapRequestHandler : IRequestHandler<ConvertFourImagesToNormalMapRequest, SKBitmap>
{
    public async Task<SKBitmap> Handle(ConvertFourImagesToNormalMapRequest request, CancellationToken cancellationToken)
    {
        return await ImageEffects.CreateNormalImage(request.topImage, request.rightImage, request.bottomImage, request.leftImage);
    }
}

