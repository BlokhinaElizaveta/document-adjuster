using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal class SimpleBinarizationService : IBinarizationService
    {
        public KrecImage MakeBinarized(KrecImage image)
        {
            const int limit = 150;
            var grayImage = image.ToGrayscaled();

            for (var lineIdx = 0; lineIdx < grayImage.Height; lineIdx++)
            {
                for (int counter = 0, sourceIdx = lineIdx * grayImage.BytesPerLine; counter < grayImage.Width; counter++, sourceIdx ++)
                {
                    grayImage.ImageData[sourceIdx] = grayImage.ImageData[sourceIdx] > limit 
                        ? (byte) 255 
                        : (byte) 0;
                }
            }

            return grayImage;
        }
    }
}
