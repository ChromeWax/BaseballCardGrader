using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessor.Features.AnalyzeImageForDefects;

public record AnalyzeImageForDefectsRequest(string ModelFilePath, string ImageFilePath) : IRequest<Image<Rgb24>>;