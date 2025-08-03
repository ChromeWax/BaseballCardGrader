using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessor.Features.AnnotateImageForDefects;

public record AnnotateImageForDefectsRequest(string ModelFilePath, string ImageFilePath) : IRequest<Image<Rgb24>>;