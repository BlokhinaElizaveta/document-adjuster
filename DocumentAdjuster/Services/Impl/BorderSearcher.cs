using System;
using System.Collections.Generic;
using System.Drawing;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    // https://habr.com/ru/post/114452/
    internal class BorderSearcher : IBorderSearcher
    {
        private readonly List<Point> borderPixels = new List<Point>();
        public KrecImage Search(KrecImage image, int[,] maskX, int[,] maskY)
        {
            var width = image.Width;
            var height = image.Height;
            var result = new KrecImage(image, new byte[image.ImageData.Length]);

            const int limit = 128 * 128;
            for (var lineIdx = 1; lineIdx < height - 1; lineIdx++)
            {
                for (var counter = 1; counter < width - 1; counter++)
                {
                    var gX = 0;
                    var gY = 0;
                    for (var i = 0; i < 3; i++)
                    {
                        var y = lineIdx + i - 1;
                        var sourceIdx = y * image.BytesPerLine + counter;
                        for (var j = 0; j < 3; j++)
                        {                         
                            var pixel = image.ImageData[sourceIdx + j - 1];
                            gX += maskX[i, j] * pixel;
                            gY += maskY[i, j] * pixel;
                        }
                    }

                    byte color = 0;
                    if (gX * gX + gY * gY > limit)
                    {
                        color = 255;                        
                        borderPixels.Add(new Point(counter, lineIdx));
                    }

                    result.ImageData[lineIdx * image.BytesPerLine + counter] = color;
                }
            }
            return result;
        }

        public List<Point> GetPoints() => borderPixels;
    }
}
