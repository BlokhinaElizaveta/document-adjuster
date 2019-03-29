using System;
using System.Collections.Generic;
using System.Drawing;

namespace DocumentAdjuster.Services
{
    internal class CornerFinder : ICornerFinder
    {
        public List<Point> FindCorner(Tuple<int, int>[] borders, int width, int height)
        {
            var result = new List<Point>();
            var allPoints = new HashSet<Point>();
            foreach (var line in borders)
            {
                var theta = line.Item1 * Math.PI / 180.0;
                var r = line.Item2;
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        if ((int) Math.Round(y * Math.Sin(theta) + x * Math.Cos(theta)) == r)
                        {
                            var point = new Point(x, y);
                            if (allPoints.Contains(point))
                                result.Add(point);
                            allPoints.Add(point);
                        }
                    }
                }
            }

            return result;
        }
    }
}
