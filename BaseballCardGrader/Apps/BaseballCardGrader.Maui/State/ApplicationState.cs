using SkiaSharp;

namespace BaseballCardGrader.Maui.State;

/// <summary>
/// Represents the state of the application, including the current pipeline step and a mapping of image positions to SKBitmap objects.
/// </summary>
public class ApplicationState
{
    /// <summary>
    /// The current step in the processing pipeline.
    /// </summary>
    public PipelineStep PipelineStep { get; set; } = PipelineStep.ConnectToEsp32;
    
    /// <summary>
    /// A dictionary mapping image positions to their corresponding SKBitmap objects.
    /// </summary>
    public Dictionary<ImagePosition, SKBitmap> ImagePositionToSkBitmap { get; set; } = new();
}
