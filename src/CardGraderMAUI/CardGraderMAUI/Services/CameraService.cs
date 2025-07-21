using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGraderMAUI.Services
{
    public class CameraService
    {
        public async Task<FileResult?> CapturePhotoAsync()
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                return await MediaPicker.Default.CapturePhotoAsync();
            }
            throw new NotSupportedException("Camera capture is not supported on this device.");
        }
        public async Task<Stream> CaptureImageWithOverlayAsync(Dictionary<string, Stream> imageFiles)
        {
            
            var stream = new MemoryStream();
            await Task.Delay(1000);
            return stream;
        }
    }
}
