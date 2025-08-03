using ImageProcessor.DependencyInjection;
using ImageProcessor.Features.AnnotateImageForDefects;
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
            System.Console.WriteLine("Usage: BaseballCardGrader.Console.exe <input model file> <original image file> <processed image file> <output image path>");
            return;
        }
        
        var modelFilePath = args[0];
        var originalImageFilePath = args[1];
        var processedImageFilePath = args[2];
        var outputImagePath = args[3];
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddImageProcessor();

        var provider = serviceCollection.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new AnnotateImageForDefectsRequest(modelFilePath, originalImageFilePath, processedImageFilePath));
        await result.SaveAsPngAsync(outputImagePath);
    }
}