using ImageProcessor.DependencyInjection;
using ImageProcessor.Features.AnnotateImageForDefects;
using ImageProcessor.Features.ConvertImageToOverlay;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;

namespace BaseballCardGrader.Console;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 4)
        {
            System.Console.WriteLine("Usage: BaseballCardGrader.Console.exe <input model file> <original image files dir> <processed image file> <output image path>");
            return;
        }
        
        var modelFilePath = args[0];
        var originalImageDirFilePath = args[1];
        var processedImageFilePath = args[2];
        var outputImagePath = args[3];
        
        if (!Directory.Exists(originalImageDirFilePath))
        {
            System.Console.WriteLine($"Error: Directory '{originalImageDirFilePath}' does not exist.");
            return;
        }
        
        var files = Directory.GetFiles(originalImageDirFilePath)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        string? top = files.FirstOrDefault(f => f.Contains("top", StringComparison.OrdinalIgnoreCase));
        string? bottom = files.FirstOrDefault(f => f.Contains("bottom", StringComparison.OrdinalIgnoreCase));
        string? left = files.FirstOrDefault(f => f.Contains("left", StringComparison.OrdinalIgnoreCase));
        string? right = files.FirstOrDefault(f => f.Contains("right", StringComparison.OrdinalIgnoreCase));

        // Check if any file is missing
        if (top is null || bottom is null || left is null || right is null)
        {
            System.Console.WriteLine("Error: Could not find one or more required images (top, bottom, left, right).");
            return;
        }
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddImageProcessor();

        var provider = serviceCollection.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();
        
        await sender.Send(new ConvertImageToOverlayRequest(top, bottom, right, left, processedImageFilePath));

        var result = await sender.Send(new AnnotateImageForDefectsRequest(modelFilePath, top, processedImageFilePath));
        await result.SaveAsPngAsync(outputImagePath);
    }
}