using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateFromFourImagesForDefects;

public record AnnotateFromFourImagesForDefectsRequest(MemoryStream ModelMemoryStream, SKBitmap OriginalImage, SKBitmap TopImage, SKBitmap RightImage, SKBitmap BottomImage, SKBitmap LeftImage) : IRequest<SKBitmap>;