namespace ImageProcessor.Features.AnalyzeImageForDefects;

public static class Constants
{
    public const int ResizeImageWidth = 800;
    public const int ResizeImageHeight = 1120;
    public const string TensorName = "input";
    public const int ChannelCount = 3;
    public const int ChannelRed = 0;
    public const int ChannelGreen = 1;
    public const int ChannelBlue = 2;
    public const float MaxIntensityPerChannel = 255f;
    public const int CurrentBatch = 0;
    public const int BatchSize = 1;
    public const float ScoreThreshold = 0.5f;
}