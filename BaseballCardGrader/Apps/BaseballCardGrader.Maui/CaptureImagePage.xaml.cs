using SixLabors.ImageSharp;
namespace BaseballCardGrader.Maui;

public partial class CaptureImagePage : ContentPage
{
    // Holds the original JPEG bytes in memory
    private List<byte[]> capturedJpegs = new();

    // Holds ImageSources for UI preview
    private List<ImageSource> capturedImages = new();

    public CaptureImagePage()
    {
        InitializeComponent();
    }

    private bool isTakingPhotos = false;

    private async void OnTakeFourPhotosClicked(object sender, EventArgs e)
    {
        if (isTakingPhotos)
            return; // Prevent re-entrancy

        try
        {
            isTakingPhotos = true;

            // Clear previous photos
            capturedJpegs.Clear();
            capturedImages.Clear();
            imagePreviewStack.Children.Clear();

            // Disable the button to prevent clicks during capture
            ((Button)sender).IsEnabled = false;

            for (int i = 0; i < 4; i++)
            {
                using var photoStream = await cameraView.CaptureImage(CancellationToken.None);
                if (photoStream != null)
                {
                    if (photoStream.CanSeek)
                        photoStream.Position = 0;
                    
                    using var ms = new MemoryStream();
                    await photoStream.CopyToAsync(ms);
                    var jpegBytes = ms.ToArray();

                    capturedJpegs.Add(jpegBytes);

                    var imageSource = ImageSource.FromStream(() => new MemoryStream(jpegBytes));
                    capturedImages.Add(imageSource);

                    var previewImage = new Microsoft.Maui.Controls.Image
                    {
                        Source = imageSource,
                        WidthRequest = 100,
                        HeightRequest = 100,
                        Aspect = Aspect.AspectFill
                    };

                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, args) => ShowFullImage(imageSource);
                    previewImage.GestureRecognizers.Add(tapGesture);

                    imagePreviewStack.Children.Add(previewImage);
                }

                await Task.Delay(500);
            }
        }
        finally
        {
            isTakingPhotos = false;
            ((Button)sender).IsEnabled = true; // Re-enable button after done
        }
    }
    private async void ShowFullImage(ImageSource imageSource)
    {
        var fullImagePage = new ContentPage
        {
            BackgroundColor = Colors.Black, Content = new Grid
            {
                Children =
                {
                    new Microsoft.Maui.Controls.Image
                    {
                        Source = imageSource, Aspect = Aspect.AspectFit, VerticalOptions = LayoutOptions.Center
                        , HorizontalOptions = LayoutOptions.Center
                    }
                }
            }
        };

        var tapToClose = new TapGestureRecognizer();
        tapToClose.Tapped += async (s, e) => await Navigation.PopModalAsync();
        fullImagePage.Content.GestureRecognizers.Add(tapToClose);

        await Navigation.PushModalAsync(fullImagePage);
    }

    private async void OnEvaluateClicked(object sender, EventArgs e)
    {
        var evaluatedImage = await evaluationView.getEvaluatedImage(capturedJpegs);

        using var ms = new MemoryStream();
        await evaluatedImage.SaveAsPngAsync(ms); // You could also use SaveAsJpegAsync
        ms.Position = 0;

        evaluationView.SetEvaluationImage(ImageSource.FromStream(() => new MemoryStream(ms.ToArray())));
        evaluationView.IsVisible = true;
    }
}
