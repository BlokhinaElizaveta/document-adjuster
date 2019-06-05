using System;
using System.Collections.Generic;
using System.Drawing;
using DocumentAdjuster.Models;

namespace DocumentAdjuster.Services
{
    // https://habr.com/ru/post/102948/
    // http://robocraft.ru/blog/computervision/502.html
    internal class EquationOfLineFinder : IEquationOfLineFinder
    {
        public EquationOfLine[] GetLines(List<Point> points, int count, int width, int height)
        {
            const double accuracy = 0.4;
            var diagonalLength = (int) Math.Ceiling(Math.Sqrt(width * width + height * height));
            var accumulator = new int[360, diagonalLength];

            foreach (var point in points)
            {
                for (var f = 0; f < 360; f++)
                {
                    var theta = f * Math.PI / 180.0;
                    var a = point.Y * Math.Sin(theta) + point.X * Math.Cos(theta);
                    for (var r = 0; r < diagonalLength; r += 5)
                    {
                        if (Math.Abs(a - r) < accuracy)
                            accumulator[f, r]++;
                    }
                }
            }

            var result = new EquationOfLine[count];
            for (var i = 0; i < count; i++)
            {
                var rMax = 0;
                var fMax = 0;
                var max = 0;
                for (var f = 0; f < accumulator.GetLength(0); f++)
                {
                    for (var r = 0; r < accumulator.GetLength(1); r++)
                    {
                        if (accumulator[f, r] <= max)
                            continue;

                        max = accumulator[f, r];
                        rMax = r;
                        fMax = f;
                    }
                }

                result[i] = new EquationOfLine {Angle = fMax, Radius = rMax};
                accumulator[fMax, rMax] = 0;
            }

            return result;
        }
    }
}