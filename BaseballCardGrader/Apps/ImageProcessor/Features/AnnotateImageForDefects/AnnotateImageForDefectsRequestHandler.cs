using Mediator;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessor.Features.AnnotateImageForDefects;

public class AnnotateImageForDefectsRequestHandler : IRequestHandler<AnnotateImageForDefectsRequest, Image<Rgb24>>
{
    public Task<Image<Rgb24>> Handle(AnnotateImageForDefectsRequest request, CancellationToken cancellationToken)
    {
        // Read image
        var originalImage = request.OriginalImage;
        var overlayImage = request.OverlayImage;
        
        // Gets original image dimensions
        var originalImageWidth = originalImage.Width;
        var originalImageHeight = originalImage.Height;

        // Resize image
        originalImage.Mutate(x => x.Resize(Constants.ResizeImageWidth, Constants.ResizeImageHeight));
        overlayImage.Mutate(x => x.Resize(Constants.ResizeImageWidth, Constants.ResizeImageHeight));

        // Preprocess image
        Tensor<float> input = new DenseTensor<float>([
            Constants.BatchSize, 
            Constants.ChannelCount, 
            Constants.ResizeImageHeight, 
            Constants.ResizeImageWidth
        ]);
        overlayImage.ProcessPixelRows(accessor =>
        {
            for (var currentYPosition = 0; currentYPosition < Constants.ResizeImageHeight; currentYPosition++)
            {
                var pixelSpan = accessor.GetRowSpan(currentYPosition);
                for (var currentXPosition = 0; currentXPosition < Constants.ResizeImageWidth; currentXPosition++)
                {
                    input[Constants.CurrentBatch, Constants.ChannelRed, currentYPosition, currentXPosition]
                        = pixelSpan[currentXPosition].R / Constants.MaxIntensityPerChannel; 
                    input[Constants.CurrentBatch, Constants.ChannelGreen, currentYPosition, currentXPosition]
                        = pixelSpan[currentXPosition].G / Constants.MaxIntensityPerChannel; 
                    input[Constants.CurrentBatch, Constants.ChannelBlue, currentYPosition, currentXPosition] 
                        = pixelSpan[currentXPosition].B / Constants.MaxIntensityPerChannel; 
                }
            }
        });

        // Setup inputs and outputs
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(Constants.TensorName, input)
        };

        // Run inference
        using var session = new InferenceSession(request.ModelFilePath);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

        // Postprocess to get predictions
        var boxes = results.First(x => x.Name == "boxes").AsTensor<float>().ToArray();
        var labels = results.First(x => x.Name == "labels").AsTensor<long>().ToArray();
        var scores = results.First(x => x.Name == "scores").AsTensor<float>().ToArray();
        var masks = results.First(x => x.Name == "masks").AsTensor<float>().ToArray();

        // Annotate image with masks for scores > 0.5
        for (var maskIdx = 0; maskIdx < scores.Length; maskIdx++)
        {
            if (scores[maskIdx] <= Constants.ScoreThreshold) continue;

            var maskOffset = maskIdx * Constants.ResizeImageHeight * Constants.ResizeImageWidth;
            for (var i = 0; i < Constants.ResizeImageHeight * Constants.ResizeImageWidth; i++)
            {
                if (masks[maskOffset + i] <= Constants.ScoreThreshold) continue;

                var y = i / Constants.ResizeImageWidth;
                var x = i % Constants.ResizeImageWidth;

                var pixel = originalImage[x, y];
                originalImage[x, y] = new Rgb24(
                    (byte)Math.Min(Constants.MaxIntensityPerChannel, pixel.R + Constants.MaskIntensity),
                    pixel.G,
                    pixel.B
                );
            }
        }

        return Task.FromResult(originalImage);
    }
}