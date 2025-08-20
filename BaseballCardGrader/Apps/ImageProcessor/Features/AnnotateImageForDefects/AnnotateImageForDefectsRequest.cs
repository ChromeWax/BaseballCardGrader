using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateImageForDefects;

public record AnnotateImageForDefectsRequest(MemoryStream ModelMemoryStream, SKBitmap topImage, SKBitmap rightImage, SKBitmap bottomImage, SKBitmap leftImage) : IRequest<SKBitmap>;