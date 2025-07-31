using MyMediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BaseballCardGrader.Onnx.Features.AnalyzeImageForDefects;

public record AnalyzeImageForDefectsRequest(string ModelFilePath, string ImageFilePath) : IRequest<Image<Rgb24>>;