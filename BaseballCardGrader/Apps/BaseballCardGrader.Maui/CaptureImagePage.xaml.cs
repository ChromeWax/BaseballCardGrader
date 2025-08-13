using BaseballCardGrader.Maui.Services;
using BaseballCardGrader.Maui.State;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BaseballCardGrader.Maui;

public partial class CaptureImagePage : ContentPage
{
    private readonly ApplicationState _appState;
    private readonly Action? _onCaptureCompleted;
    private readonly IImageConversionService _imageConversionService;

    // Holds the original JPEG bytes in memory
    private List<byte[]> capturedJpegs = new();

    // Holds ImageSources for UI preview
    private List<ImageSource> capturedImages = new();

    public CaptureImagePage(ApplicationState appState, IImageConversionService imageConversionService, Action? onCaptureCompleted)
    {
        InitializeComponent();
        _appState = appState;
        _imageConversionService = imageConversionService;
        _onCaptureCompleted = onCaptureCompleted;
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
            
            // Now populate ApplicationState properties from capturedJpegs
            if (capturedJpegs.Count == 4)
            {
                _appState.jpegTopImage = capturedJpegs[0];
                _appState.jpegBottomImage = capturedJpegs[1];
                _appState.jpegLeftImage = capturedJpegs[2];
                _appState.jpegRightImage = capturedJpegs[3];
                
                var tasks = new Task<Image<L8>>[]
                {
                    Task.Run(() => _imageConversionService.ConvertJpegBytesToGrayscaleImage(_appState.jpegTopImage)),
                    Task.Run(() => _imageConversionService.ConvertJpegBytesToGrayscaleImage(_appState.jpegBottomImage)),
                    Task.Run(() => _imageConversionService.ConvertJpegBytesToGrayscaleImage(_appState.jpegLeftImage)),
                    Task.Run(() => _imageConversionService.ConvertJpegBytesToGrayscaleImage(_appState.jpegRightImage))
                };

                var results = await Task.WhenAll(tasks);
                _appState.GrayscaleTopImage = results[0];
                _appState.GrayscaleBottomImage = results[1];
                _appState.GrayscaleLeftImage = results[2];
                _appState.GrayscaleRightImage = results[3];
                
                Console.WriteLine("Finished converting all Images to GrayscaleImages");
                
                CaptureCompleted();
            }
        }
        finally
        {
            isTakingPhotos = false;
            ((Button)sender).IsEnabled = true; // Re-enable button after done
        }
    }

    private async void CaptureCompleted()
    {
        _onCaptureCompleted?.Invoke();
        // Navigate back
        await Navigation.PopAsync();
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

}
