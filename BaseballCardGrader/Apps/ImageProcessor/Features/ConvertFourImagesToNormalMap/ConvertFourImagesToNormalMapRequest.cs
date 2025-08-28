using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.ConvertFourImagesToNormalMap;

public record ConvertFourImagesToNormalMapRequest(SKBitmap topImage, SKBitmap rightImage, SKBitmap bottomImage, SKBitmap leftImage) : IRequest<SKBitmap>;