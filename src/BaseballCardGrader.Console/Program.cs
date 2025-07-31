using ImageProcessor.DependencyInjection;
using ImageProcessor.Features.AnalyzeImageForDefects;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace BaseballCardGrader.Console;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            System.Console.WriteLine("Usage: BaseballCardGrader.Console.exe <input model file> <input image file> <output image path>");
            return;
        }
        
        var modelFilePath = args[0];
        var imageFilePath = args[1];
        var imageFilePath = args[2];
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddImageProcessor();

        var provider = serviceCollection.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new AnalyzeImageForDefectsRequest(modelFilePath, imageFilePath));
        result.Save(outputImagePath);
    }
}