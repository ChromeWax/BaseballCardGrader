using SkiaSharp;

namespace BaseballCardGrader.Maui.State;

public class ApplicationState
{
    public PipelineStep PipelineStep { get; set; } = PipelineStep.ConnectToEsp32;
    public Dictionary<ImagePosition, SKBitmap> ImagePositionToSkBitmap { get; set; } = new();
}
