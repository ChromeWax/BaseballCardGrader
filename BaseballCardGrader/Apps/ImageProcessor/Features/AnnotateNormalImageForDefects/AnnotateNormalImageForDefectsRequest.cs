using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateNormalImageForDefects;

public record AnnotateNormalMapForDefectsRequest(MemoryStream ModelMemoryStream, SKBitmap OriginalImage, SKBitmap NormalMap) : IRequest<SKBitmap>;