using Mediator;

namespace ImageProcessor.Features.ConvertImageToOverlay;

public record ConvertImageToOverlayRequest(string originalTopImagePath, string originalBottomImagePath, string originalRightImagePath, string originalLeftImagePath, string outputFilePath) : IRequest<string>;
