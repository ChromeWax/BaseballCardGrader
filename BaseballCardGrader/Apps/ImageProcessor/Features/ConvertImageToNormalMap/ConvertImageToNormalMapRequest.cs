using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessor.Features.ConvertImageToNormalMap;

public record ConvertImageToNormalMapRequest(
    string originalTopImagePath, string originalBottomImagePath, string originalRightImagePath, string originalLeftImagePath) : IRequest<Image<Rgb24>>;