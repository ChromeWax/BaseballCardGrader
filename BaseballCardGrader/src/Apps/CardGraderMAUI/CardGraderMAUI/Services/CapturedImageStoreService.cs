using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGraderMAUI.Services
{
    public class CapturedImageStoreService
    {
        private readonly Dictionary<string, byte[]> _images = new();

        public void SaveImage(string key, byte[] imageBytes)
        {
            _images[key] = imageBytes;
        }

        public byte[]? GetImage(string key)
        {
            _images.TryGetValue(key, out var image);
            return image;
        }

        public bool HasAllFourImages()
        {
            return _images.ContainsKey("upImg") &&
                   _images.ContainsKey("downImg") &&
                   _images.ContainsKey("leftImg") &&
                   _images.ContainsKey("rightImg");
        }

        public Dictionary<string, byte[]> GetAllImages() => new(_images);
    }
}
