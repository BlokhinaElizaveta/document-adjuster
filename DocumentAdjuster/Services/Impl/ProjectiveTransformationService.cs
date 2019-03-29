using System;
using System.Collections.Generic;
using System.Drawing;
using Accord.Math;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal class ProjectiveTransformationService : IProjectiveTransformationService
    {
        public KrecImage ApplyTransformMatrix(KrecImage image, List<Point> corners)
        {
            var matrix = GetMatrix(corners).Inverse();
            var width = image.Width;
            var height = image.Height;
            var result = new KrecImage(image, new byte[image.ImageData.Length]);
            for (var lineIdx = 0; lineIdx < height; lineIdx++)
            {
                for (int counter = 0, sourceIdx = lineIdx * image.BytesPerLine;
                    counter < width;
                    counter++, sourceIdx += 3)
                {
                    var point = new double[,] {{counter, lineIdx, 1}};
                    var newPoint = point.Dot(matrix);
                    var newX = (int) Math.Round(newPoint[0, 0] / newPoint[0, 2] * width);
                    var newY = (int) Math.Round(newPoint[0, 1] / newPoint[0, 2] * height);

                    if (newX > 0 && newX < width && newY > 0 && newY < height)
                    {
                        var index = newY * image.BytesPerLine + newX * 3;
                        result.ImageData[index] = image.ImageData[sourceIdx];
                        result.ImageData[index + 1] = image.ImageData[sourceIdx + 1];
                        result.ImageData[index + 2] = image.ImageData[sourceIdx + 2];
                    }
                }
            }

            return ApplySmoothingFilter(result);
            //return result;
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

        private static KrecImage ApplySmoothingFilter(KrecImage image)
        {
            var result = new KrecImage(image, new byte[image.ImageData.Length]);
            var bytesPerPixel = 3;
            for (var lineIdx = bytesPerPixel; lineIdx < image.Height - bytesPerPixel; lineIdx++)
            {
                for (var counter = bytesPerPixel; counter < image.Width - bytesPerPixel; counter++)
                {
                    var neighbors = new List<Tuple<byte, byte, byte>>();
                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = 0; j < 3; j++)
                        {
                            var y = lineIdx + i - 1;
                            var sourceIdx = y * image.BytesPerLine + (counter + j - 1) * bytesPerPixel;
                            neighbors.Add(Tuple.Create(
                                image.ImageData[sourceIdx],
                                image.ImageData[sourceIdx + 1],
                                image.ImageData[sourceIdx + 2]));
                        }
                    }

                    var index = lineIdx * image.BytesPerLine + counter * bytesPerPixel;
                    var averageWithoutBlack = GetAverageWithoutBlack(neighbors);
                    result.ImageData[index] = averageWithoutBlack.Item1;
                    result.ImageData[index + 1] = averageWithoutBlack.Item2;
                    result.ImageData[index + 2] = averageWithoutBlack.Item3;
                }
            }

            return result;
        }

        private static Tuple<byte, byte, byte> GetAverageWithoutBlack(IEnumerable<Tuple<byte, byte, byte>> neighbors)
        {
            var r = 0;
            var g = 0;
            var b = 0;
            var countR = 0;
            var countG = 0;
            var countB = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.Item1 != 0)
                {
                    r += neighbor.Item1;
                    countR++;
                }

                if (neighbor.Item2 != 0)
                {
                    g += neighbor.Item2;
                    countG++;
                }

                if (neighbor.Item3 != 0)
                {
                    b += neighbor.Item3;
                    countB++;
                }
            }

            var rAverage = countR == 0 ? (byte) 0 : (byte) (r / countR);
            var gAverage = countG == 0 ? (byte) 0 : (byte) (g / countG);
            var bAverage = countB == 0 ? (byte) 0 : (byte) (b / countB);

            return new Tuple<byte, byte, byte>(rAverage, gAverage, bAverage);
        }
    }
}
