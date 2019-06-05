using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal class PerspectiveTransformation : IPerspectiveTransformation
    {
        public KrecImage ApplyTransformMatrix(KrecImage image, List<Point> corners)
        {
            var matrix = GetMatrix(corners);
            var width = image.Width;
            var height = image.Height;
            var bytesPerPixel = image.Format.BytesPerPixel();
            var result = new KrecImage(image, new byte[image.ImageData.Length]);
            for (var lineIdx = 0; lineIdx < height; lineIdx++)
            {
                for (int counter = 0, sourceIdx = lineIdx * image.BytesPerLine;
                    counter < width;
                    counter++, sourceIdx += bytesPerPixel)
                {
                    var point = new [,] {{(double)counter / width, (double)lineIdx/height, 1}};

                    var color = GetСolor(MultiplyMatrix(point, matrix), image, bytesPerPixel);

                    for (var i = 0; i < bytesPerPixel; i++)
                        result.ImageData[sourceIdx + i] = color[i];
                }
            }
            return result;
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

        private byte[] GetСolor(double[,] doublePoint, KrecImage image, int bytesPerPixel)
        {
            var doublePointX = doublePoint[0, 0] / doublePoint[0, 2];
            var doublePointY = doublePoint[0, 1] / doublePoint[0, 2];
            var neighboringPoints = new []
            {
                new Point((int)Math.Floor(doublePointX), (int)Math.Floor(doublePointY)),
                new Point((int)Math.Floor(doublePointX), (int)Math.Ceiling(doublePointY)),
                new Point((int)Math.Ceiling(doublePointX), (int)Math.Floor(doublePointY)),
                new Point((int)Math.Ceiling(doublePointX), (int)Math.Ceiling(doublePointY))
            };
            var distanceToNeighbors = neighboringPoints.Select(p =>
                Math.Sqrt((doublePointX - p.X) * (doublePointX - p.X) +
                          (doublePointY - p.Y) * (doublePointY - p.Y))).ToArray();
            var sumOfDistances = distanceToNeighbors.Sum();

            var weightNeighbors = new double[4];
            for (var i = 0; i < distanceToNeighbors.Length; i++)
                weightNeighbors[i] = 0.5 - distanceToNeighbors[i] / sumOfDistances;

            var color = new byte[bytesPerPixel];

            for (var pointIndex = 0; pointIndex < neighboringPoints.Length; pointIndex++)
            {
                var neighboringPoint = neighboringPoints[pointIndex];
                if (neighboringPoint.X > 0 && neighboringPoint.X < image.Width && neighboringPoint.Y > 0 && neighboringPoint.Y < image.Height)
                {
                    var index = neighboringPoint.Y * image.BytesPerLine + neighboringPoint.X * bytesPerPixel;
                    for (var i = 0; i < bytesPerPixel; i++)
                        color[i] += (byte)(image.ImageData[index + i] * weightNeighbors[pointIndex]);
                }
            }

            return color;
        }

        private static double[,] MultiplyMatrix(double[,] a, double[,] b)
        {
            var c = new double[a.GetLength(0), b.GetLength(1)];
            for (var i = 0; i < c.GetLength(0); i++)
            {
                for (var j = 0; j < c.GetLength(1); j++)
                {
                    for (var k = 0; k < a.GetLength(1); k++)
                        c[i, j] = c[i, j] + a[i, k] * b[k, j];
                }
            }

            return c;
        }
    }
}
