using Mediator;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessor.Features.AnalyzeImageForDefects;

public class AnalyzeImageForDefectsRequestHandler : IRequestHandler<AnalyzeImageForDefectsRequest, Image<Rgb24>>
{
    public Task<Image<Rgb24>> Handle(AnalyzeImageForDefectsRequest request, CancellationToken cancellationToken)
    {
        // Read image
        var image = Image.Load<Rgb24>(request.ImageFilePath);

        // Resize image
        image.Mutate(x => x.Resize(Constants.ResizeImageWidth, Constants.ResizeImageHeight));

        // Preprocess image
        Tensor<float> input = new DenseTensor<float>(new[] { 3, Constants.ResizeImageHeight, Constants.ResizeImageWidth });
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < Constants.ResizeImageHeight; y++)
            {
                Span<Rgb24> pixelSpan = accessor.GetRowSpan(y);
                for (int x = 0; x < Constants.ResizeImageWidth; x++)
                {
                    input[0, y, x] = pixelSpan[x].R / 255f; // Red channel
                    input[1, y, x] = pixelSpan[x].G / 255f; // Green channel
                    input[2, y, x] = pixelSpan[x].B / 255f; // Blue channel
                }
            }
        });

        // Setup inputs and outputs
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", input)
        };

        // Run inference
        using var session = new InferenceSession(request.ModelFilePath);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

        // Postprocess to get predictions
        var boxes = results.First(x => x.Name == "boxes").AsTensor<float>().ToArray();
        var labels = results.First(x => x.Name == "labels").AsTensor<long>().ToArray();
        var scores = results.First(x => x.Name == "scores").AsTensor<float>().ToArray();
        var masks = results.First(x => x.Name == "masks").AsTensor<float>().ToArray();

        return Task.FromResult(image);
    }
}