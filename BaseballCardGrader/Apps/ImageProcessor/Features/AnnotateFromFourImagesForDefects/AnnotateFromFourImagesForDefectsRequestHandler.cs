using ImageProcessor.Helper.ImageEffects;
using ImageProcessor.Helper.Inference;
using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateFromFourImagesForDefects;

public class AnnotateFromFourImagesForDefectsRequestHandler : IRequestHandler<AnnotateFromFourImagesForDefectsRequest, SKBitmap>
{
    public async Task<SKBitmap> Handle(AnnotateFromFourImagesForDefectsRequest request, CancellationToken cancellationToken)
    {
        var normalMap = await ImageEffects.CreateOverlayImage(request.TopImage, request.RightImage, request.BottomImage, request.LeftImage);
        var annotatedImage = AnnotateImage.AnnotateImageWithSegmentationMask(request.ModelMemoryStream, request.OriginalImage, normalMap);
        return annotatedImage;
    }
}