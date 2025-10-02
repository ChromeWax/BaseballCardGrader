using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.ConvertFourImagesToNormalMap;

/// <summary>
/// Request to convert four directional images into a normal map.
/// </summary>
/// <param name="TopImage">Original top RGB image.</param>
/// <param name="RightImage">Original right RGB image.</param>
/// <param name="BottomImage">Original bottom RGB image.</param>
/// <param name="LeftImage">Original left RGB image.</param>
public record ConvertFourImagesToNormalMapRequest(SKBitmap TopImage, SKBitmap RightImage, SKBitmap BottomImage, SKBitmap LeftImage) : IRequest<SKBitmap>;