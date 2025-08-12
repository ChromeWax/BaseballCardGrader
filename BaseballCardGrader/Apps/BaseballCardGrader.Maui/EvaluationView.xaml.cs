using ImageProcessor.DependencyInjection;
using ImageProcessor.Features.AnnotateImageForDefects;
using ImageProcessor.Features.ConvertImageToOverlay;
using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace BaseballCardGrader.Maui;

public partial class EvaluationView : ContentView
{
    public EvaluationView()
    {
        InitializeComponent();
    }

    public async Task<Image<Rgb24>> getEvaluatedImage(List<byte[]> jpegs)
    {
        var topImage = Image.Load<L8>(jpegs[0]);
        var bottomImage = Image.Load<L8>(jpegs[1]);
        var leftImage = Image.Load<L8>(jpegs[2]);
        var rightImage = Image.Load<L8>(jpegs[3]);
        var originalImageRgb = Image.Load<Rgb24>(jpegs[0]);
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddImageProcessor();

        var provider = serviceCollection.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();
        var overlayImage = await sender.Send(new ConvertImageToOverlayRequest(topImage, bottomImage, rightImage, leftImage));
        var modelOutput = await sender.Send(new AnnotateImageForDefectsRequest("BaseballCardGrader.Maui/Resources/Raw/BaseballCardGraderModel.onnx", originalImageRgb, overlayImage));
        // await modelOutput.SaveAsPngAsync("BaseballCardGrader.Maui/Resources/Images/ModelOutput.png");
        return modelOutput;
    }
    
    public void SetEvaluationImage(ImageSource source)
    {
        evaluationImage.Source = source;
    }
}
