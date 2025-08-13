using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
namespace BaseballCardGrader.Maui.State;

public class ApplicationState
{
    public Image<Rgb24> RgbTopImage { get; set; }
    public Image<Rgb24> RgbBottomImage { get; set; }
    public Image<Rgb24> RgbLeftImage { get; set; }
    public Image<Rgb24> RgbRightImage { get; set; }
    
    public Image<L8> GrayscaleTopImage { get; set; }
    public Image<L8> GrayscaleBottomImage { get; set; }
    public Image<L8> GrayscaleLeftImage { get; set; }
    public Image<L8> GrayscaleRightImage { get; set; }
    
    public byte[] jpegTopImage { get; set; }
    public byte[] jpegBottomImage { get; set; }
    public byte[] jpegLeftImage { get; set; }
    public byte[] jpegRightImage { get; set; }
    
    
}
