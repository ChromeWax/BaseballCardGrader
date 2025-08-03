using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessor.Features.AnnotateImageForDefects;

public record AnnotateImageForDefectsRequest(string ModelFilePath, string OriginalImageFilePath, string ProcessedImageFilePath) : IRequest<Image<Rgb24>>;