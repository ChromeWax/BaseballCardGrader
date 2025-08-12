using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessor.Features.ConvertImageToOverlay;

public record ConvertImageToOverlayRequest(Image<L8> originalTopImage, Image<L8> originalBottomImage, Image<L8> originalRightImage, Image<L8> originalLeftImage) : IRequest<Image<Rgb24>>;
