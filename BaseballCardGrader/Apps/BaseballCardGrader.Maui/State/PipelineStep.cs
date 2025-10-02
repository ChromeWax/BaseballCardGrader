namespace BaseballCardGrader.Maui.State;

/// <summary>
/// Enum representing the steps in the processing pipeline.
/// </summary>
public enum PipelineStep
{
    ConnectToEsp32,
    CaptureImages,
    ProcessImages
}