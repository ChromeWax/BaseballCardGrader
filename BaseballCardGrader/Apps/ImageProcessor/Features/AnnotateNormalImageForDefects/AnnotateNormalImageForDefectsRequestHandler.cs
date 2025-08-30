using ImageProcessor.Helper.Inference;
using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateNormalImageForDefects;

public class AnnotateNormalMapForDefectsRequestHandler : IRequestHandler<AnnotateNormalMapForDefectsRequest, SKBitmap>
{
    public async Task<SKBitmap> Handle(AnnotateNormalMapForDefectsRequest request, CancellationToken cancellationToken)
    {
        var annotatedImage = AnnotateImage.AnnotateImageWithSegmentationMask(request.ModelMemoryStream, request.OriginalImage, request.NormalMap);
        return annotatedImage;
    }
}