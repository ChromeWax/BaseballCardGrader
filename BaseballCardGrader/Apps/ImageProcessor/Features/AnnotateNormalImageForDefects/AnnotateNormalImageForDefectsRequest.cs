using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateNormalImageForDefects;

/// <summary>
/// Request to annotate an image using a normal map for defect detection.
/// </summary>
/// <param name="ModelMemoryStream">Onnx model in memory stream.</param>
/// <param name="OriginalImage">Original RGB image for annotation use.</param>
/// <param name="NormalMap">Normal map generated from 4 RGB images.</param>
public record AnnotateNormalMapForDefectsRequest(MemoryStream ModelMemoryStream, SKBitmap OriginalImage, SKBitmap NormalMap) : IRequest<SKBitmap>;