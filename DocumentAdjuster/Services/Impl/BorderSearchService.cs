using System.Collections.Generic;
using System.Drawing;

namespace DocumentAdjuster.Services
{
    internal class BorderSearchService:IBorderSearchService
    {
        private readonly List<Point> borderPixels = new List<Point>();
        public int[,] Search(int[,] image, int[,] maskX, int[,] maskY)
        {
            var width = image.GetLength(0);
            var height = image.GetLength(1);
            var result = new int[width, height];

            const int limit = 128 * 128;

            for (var x = 1; x < width - 1; x++)
            {
                for (var y = 1; y < height - 1; y++)
                {
                    var gX = 0;
                    var gY = 0;

                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = 0; j < 3; j++)
                        {
                            var pixel = image[x + j - 1, y + i - 1];
                            gX += maskX[i, j] * pixel;
                            gY += maskY[i, j] * pixel;
                        }
                    }

                    var color = 0;
                    if (gX * gX + gY * gY > limit)
                    {
                        color = 255;
                        borderPixels.Add(new Point(x, y));
                    }

                    result[x, y] = color;
                }
            }
            return result;
        }

        public List<Point> GetPoints() => borderPixels;
    }
}
