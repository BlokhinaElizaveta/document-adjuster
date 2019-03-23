using System;
using System.Collections.Generic;
using System.Drawing;
using Accord.Math;

namespace DocumentAdjuster.Services
{
    internal class ProjectiveTransformationService : IProjectiveTransformationService
    {
        public Bitmap ApplyTransformMatrix(Bitmap image, List<Point> corners)
        {
            var matrix = GetMatrix(corners).Inverse();
            var newWidth = image.Width;
            var newHeight = image.Height;
            var result = new Bitmap(newWidth, newHeight);
            for (var x = 0; x < image.Width; x++)
            {
                for (var y = 0; y < image.Height; y++)
                {
                    var point = new double[,] {{x, y, 1}};
                    var newPoint = point.Dot(matrix);
                    var pixel = image.GetPixel(x, y);
                    var newX = (int) Math.Round(newPoint[0, 0] / newPoint[0, 2] * newWidth);
                    var newY = (int) Math.Round(newPoint[0, 1] / newPoint[0, 2] * newHeight);

                    if (newX > 0 && newX < newWidth && newY > 0 && newY < newHeight)
                        result.SetPixel(newX, newY, Color.FromArgb(pixel.R, pixel.G, pixel.B));
                }
            }

            return ApplySmoothingFilter(result);
        }

        private static double[,] GetMatrix(IReadOnlyList<Point> corners)
        {
            double x1 = corners[0].X;
            double x2 = corners[1].X;
            double x3 = corners[2].X;
            double x4 = corners[3].X;
            double y1 = corners[0].Y;
            double y2 = corners[1].Y;
            double y3 = corners[2].Y;
            double y4 = corners[3].Y;

            var a13 = ((y4 - y3) * (x1 + x3 - x2 - x4) - (x4 - x3) * (y1 + y3 - y2 - y4)) /
                    ((y4 - y3) * (x2 - x3) - (x4 - x3) * (y2 - y3));
            var a23 = (x1 + x3 - x2 - x4 - (x2 - x3) * a13) / (x4 - x3);
            var a33 = 1;

            var a11 = x2 * a13 + x2 - x1;
            var a21 = x4 * a23 + x4 - x1;
            var a31 = x1;

            var a12 = y2 * a13 + y2 - y1;
            var a22 = y4 * a23 + y4 - y1;
            var a32 = y1;


            return new[,]
            {
                {a11, a12, a13},
                {a21, a22, a23},
                {a31, a32, a33}
            };
        }

        private static Bitmap ApplySmoothingFilter(Bitmap image)
        {
            var result = new Bitmap(image);
            for (var x = 1; x < image.Width - 1; x++)
            {
                for (var y = 1; y < image.Height - 1; y++)
                {
                    var neighbors = new List<Color>();
                    var pixel = image.GetPixel(x, y);
                    if (pixel.R != 0 || pixel.G != 0 || pixel.B != 0)
                        continue;

                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = 0; j < 3; j++)
                            neighbors.Add(image.GetPixel(x + j - 1, y + i - 1));
                    }

                    result.SetPixel(x, y, GetAverageWithoutBlack(neighbors));
                }
            }

            return result;
        }

        private static Color GetAverageWithoutBlack(IEnumerable<Color> neighbors)
        {
            var r = 0;
            var g = 0;
            var b = 0;
            var countR = 0;
            var countG = 0;
            var countB = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.R != 0)
                {
                    r += neighbor.R;
                    countR++;
                }

                if (neighbor.G != 0)
                {
                    g += neighbor.G;
                    countG++;
                }

                if (neighbor.B != 0)
                {
                    b += neighbor.B;
                    countB++;
                }
            }

            return countR == 0 
                ? Color.Black 
                : Color.FromArgb(r / countR, g / countG, b / countB);
        }
    }
}
