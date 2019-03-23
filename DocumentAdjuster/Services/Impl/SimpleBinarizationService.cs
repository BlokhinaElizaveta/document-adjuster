using System;
using System.Drawing;

namespace DocumentAdjuster.Services
{
    internal class SimpleBinarizationService : IBinarizationService
    {
        public int[,] MakeBinarized(Bitmap image)
        {
            var binaryImage = new int[image.Width, image.Height];
            const int limit = 150;

            for (var x = 0; x < image.Width; x++)
            {
                for (var y = 0; y < image.Height; y++)
                {
                    var color = 255;
                    var grayPixel = GetShadeOfGray(image.GetPixel(x, y));
                    if (grayPixel.R < limit && grayPixel.G < limit && grayPixel.B < limit)
                        color = 0;

                    binaryImage[x, y] = color;
                }
            }

            return binaryImage;
        }

        private static Color GetShadeOfGray(Color pixel)
        {
            var value = (int)Math.Round(0.2125 * pixel.R + 0.7154 * pixel.G + 0.0721 * pixel.B);
            return Color.FromArgb(pixel.A, value, value, value);
        }
    }
}
