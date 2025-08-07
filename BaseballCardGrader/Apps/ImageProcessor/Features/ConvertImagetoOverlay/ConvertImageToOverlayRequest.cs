using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessor.Features.ConvertImageToOverlay;

public record ConvertImageToOverlayRequest(string originalTopImagePath, string originalBottomImagePath, string originalRightImagePath, string originalLeftImagePath) : IRequest<Image<Rgb24>>;
