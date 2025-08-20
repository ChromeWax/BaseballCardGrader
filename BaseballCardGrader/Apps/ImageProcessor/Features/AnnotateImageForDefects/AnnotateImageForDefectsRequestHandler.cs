using Mediator;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace ImageProcessor.Features.AnnotateImageForDefects;

public class AnnotateImageForDefectsRequestHandler : IRequestHandler<AnnotateImageForDefectsRequest, SKBitmap>
{
    public async Task<SKBitmap> Handle(AnnotateImageForDefectsRequest request, CancellationToken cancellationToken)
    {
        var overlayImage = await CreateOverlayImage(request.topImage, request.rightImage, request.bottomImage, request.leftImage);
        var annotatedImage = await AnnotateImage(request.ModelMemoryStream, request.topImage, overlayImage);
        return annotatedImage;
    }
    
    private async Task<SKBitmap> CreateOverlayImage(SKBitmap topImage, SKBitmap rightImage, SKBitmap bottomImage, SKBitmap leftImage)
    {
        var width = topImage.Width;
        var height = topImage.Height;

        if (bottomImage.Width != width || bottomImage.Height != height ||
            leftImage.Width != width || leftImage.Height != height ||
            rightImage.Width != width || rightImage.Height != height)
            throw new ArgumentException("All input images must have the same dimensions.");

        // Run grayscale conversions in parallel
        var topGrayTask = Task.Run(() => ToGrayscaleBytes(topImage));
        var bottomGrayTask = Task.Run(() => ToGrayscaleBytes(bottomImage));
        var rightGrayTask = Task.Run(() => ToGrayscaleBytes(rightImage));
        var leftGrayTask = Task.Run(() => ToGrayscaleBytes(leftImage));

        await Task.WhenAll(topGrayTask, bottomGrayTask, rightGrayTask, leftGrayTask);

        var topGray = topGrayTask.Result;
        var bottomGray = bottomGrayTask.Result;
        var rightGray = rightGrayTask.Result;
        var leftGray = leftGrayTask.Result;

        // Run color image creations in parallel
        var aboveLeftTask = Task.Run(() => CreateColorImageFromGrays(leftGray, topGray, width, height));
        var bottomRightTask = Task.Run(() => CreateColorImageFromGrays(rightGray, bottomGray, width, height));

        await Task.WhenAll(aboveLeftTask, bottomRightTask);

        var aboveLeft = aboveLeftTask.Result;
        var bottomRight = bottomRightTask.Result;

        // Blend bottomRight over aboveLeft at 50% alpha
        using (var canvas = new SKCanvas(aboveLeft))
        using (var paint = new SKPaint { Color = SKColors.White.WithAlpha(128), FilterQuality = SKFilterQuality.Low })
        {
            canvas.DrawBitmap(bottomRight, SKPoint.Empty, paint);
        }

        return aboveLeft;
    }
    
    private byte[] ToGrayscaleBytes(SKBitmap bmp)
    {
        int width = bmp.Width;
        int height = bmp.Height;
        var gray = new byte[width * height];
        var pixels = bmp.Pixels;

        Parallel.For(0, pixels.Length, i =>
        {
            var c = pixels[i];
            gray[i] = (byte)Math.Clamp((int)Math.Round(0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue), 0, 255);
        });

        return gray;
    }

    private SKBitmap CreateColorImageFromGrays(byte[] redChannel, byte[] greenChannel, int width, int height)
    {
        var bmp = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var pixels = new SKColor[width * height];

        Parallel.For(0, pixels.Length, i =>
        {
            pixels[i] = new SKColor(redChannel[i], greenChannel[i], 0, 255);
        });

        bmp.Pixels = pixels;
        return bmp;
    }

    private async Task<SKBitmap> AnnotateImage(MemoryStream modelMemoryStream, SKBitmap originalImage, SKBitmap overlayImage)
    {
        try
        {
            int originalWidth = originalImage.Width;
            int originalHeight = originalImage.Height;

            // Resize images for model input
            var resizedOriginal = ResizeBitmap(originalImage, Constants.ResizeImageWidth, Constants.ResizeImageHeight);
            var resizedOverlay = ResizeBitmap(overlayImage, Constants.ResizeImageWidth, Constants.ResizeImageHeight);

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
            var finalImage = ResizeBitmap(resizedOriginal, originalWidth, originalHeight);

            resizedOverlay.Dispose();
            resizedOriginal.Dispose();

            return finalImage;
        }
        catch (Exception ex)
        {
            return originalImage;
        }
    }
    
    private SKBitmap ResizeBitmap(SKBitmap src, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var resized = src.Resize(info, SKFilterQuality.Medium);
        return resized;
    }
}