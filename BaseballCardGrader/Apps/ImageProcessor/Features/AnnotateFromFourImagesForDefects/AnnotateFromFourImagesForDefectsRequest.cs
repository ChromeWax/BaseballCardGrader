using Mediator;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateFromFourImagesForDefects;

/// <summary>
/// Request to annotate an image using four directional images for defect detection.
/// </summary>
/// <param name="ModelMemoryStream">Onnx model in memory stream.</param>
/// <param name="OriginalImage">Original RGB image for annotation use.</param>
/// <param name="TopImage">Original top RGB image.</param>
/// <param name="RightImage">Original right RGB image.</param>
/// <param name="BottomImage">Original bottom RGB image.</param>
/// <param name="LeftImage">Original left RGB image.</param>
public record AnnotateFromFourImagesForDefectsRequest(MemoryStream ModelMemoryStream, SKBitmap OriginalImage, SKBitmap TopImage, SKBitmap RightImage, SKBitmap BottomImage, SKBitmap LeftImage) : IRequest<SKBitmap>;