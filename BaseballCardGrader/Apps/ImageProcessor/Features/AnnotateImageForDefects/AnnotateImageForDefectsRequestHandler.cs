using ImageProcessor.Helper.ImageEffects;
using Mediator;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateImageForDefects;

public class AnnotateImageForDefectsRequestHandler : IRequestHandler<AnnotateImageForDefectsRequest, SKBitmap>
{
    public async Task<SKBitmap> Handle(AnnotateImageForDefectsRequest request, CancellationToken cancellationToken)
    {
        var overlayImage = await ImageEffects.CreateOverlayImage(request.topImage, request.rightImage, request.bottomImage, request.leftImage);
        var annotatedImage = await AnnotateImage(request.ModelMemoryStream, request.topImage, overlayImage);
        return annotatedImage;
    }
    
    private async Task<SKBitmap> AnnotateImage(MemoryStream modelMemoryStream, SKBitmap originalImage, SKBitmap overlayImage)
    {
        try
        {
            int originalWidth = originalImage.Width;
            int originalHeight = originalImage.Height;

            // Resize images for model input
            var resizedOriginal = ImageEffects.ResizeBitmap(originalImage, Constants.ResizeImageWidth, Constants.ResizeImageHeight);
            var resizedOverlay = ImageEffects.ResizeBitmap(overlayImage, Constants.ResizeImageWidth, Constants.ResizeImageHeight);

            // Prepare tensor input
            var input = new DenseTensor<float>(new[] { 
                Constants.BatchSize, 
                Constants.ChannelCount, 
                Constants.ResizeImageHeight, 
                Constants.ResizeImageWidth 
            });
            var overlayPixels = resizedOverlay.Pixels;

            Parallel.For(0, overlayPixels.Length, i =>
            {
                int y = i / Constants.ResizeImageWidth;
                int x = i % Constants.ResizeImageWidth;
                var p = overlayPixels[i];
                input[Constants.CurrentBatch, Constants.ChannelRed, y, x] = p.Red / Constants.MaxIntensityPerChannel;
                input[Constants.CurrentBatch, Constants.ChannelGreen, y, x] = p.Green / Constants.MaxIntensityPerChannel;
                input[Constants.CurrentBatch, Constants.ChannelBlue, y, x] = p.Blue / Constants.MaxIntensityPerChannel;
            });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(Constants.TensorName, input)
            };

            using var session = new InferenceSession(modelMemoryStream.ToArray());
            using var results = session.Run(inputs);

            var boxes = results.First(x => x.Name == "boxes").AsTensor<float>().ToArray();
            var labels = results.First(x => x.Name == "labels").AsTensor<long>().ToArray();
            var scores = results.First(x => x.Name == "scores").AsTensor<float>().ToArray();
            var masks = results.First(x => x.Name == "masks").AsTensor<float>().ToArray();

            var annotatedPixels = resizedOriginal.Pixels;

            Parallel.For(0, scores.Length, maskIdx =>
            {
                if (scores[maskIdx] <= Constants.ScoreThreshold) return;

                int maskOffset = maskIdx * Constants.ResizeImageHeight * Constants.ResizeImageWidth;
                Parallel.For(0, Constants.ResizeImageHeight * Constants.ResizeImageWidth, i =>
                {
                    if (masks[maskOffset + i] <= Constants.ScoreThreshold) return;

                    int y = i / Constants.ResizeImageWidth;
                    int x = i % Constants.ResizeImageWidth;
                    int idx = y * Constants.ResizeImageWidth + x;
                    var px = annotatedPixels[idx];
                    byte r = (byte)Math.Min(Constants.MaxIntensityPerChannel, px.Red + Constants.MaskIntensity);
                    annotatedPixels[idx] = new SKColor(r, px.Green, px.Blue, px.Alpha);
                });
            });

            resizedOriginal.Pixels = annotatedPixels;

            // Resize back to original size
            var finalImage = ImageEffects.ResizeBitmap(resizedOriginal, originalWidth, originalHeight);

            resizedOverlay.Dispose();
            resizedOriginal.Dispose();

            return finalImage;
        }
        catch (Exception ex)
        {
            return originalImage;
        }
    }
}