using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DocumentAdjuster.Models;

namespace DocumentAdjuster.Services
{
    internal class CornerFinder : ICornerFinder
    {
        public List<Point> FindCorner(EquationOfLine[] borders, int width, int height)
        {
            var result = new List<Point>();
            var allPoints = new HashSet<Point>();
            foreach (var line in borders)
            {
                var theta = line.Angle * Math.PI / 180.0;
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        if ((int) Math.Round(y * Math.Sin(theta) + x * Math.Cos(theta)) == line.Radius)
                        {
                            var point = new Point(x, y);
                            if (allPoints.Contains(point))
                                result.Add(point);
                            allPoints.Add(point);
                        }
                    }
                }
            }

            return SortCorners(result);
        }

        private static List<Point> SortCorners(IList<Point> corners)
        {
            var sortedCorners = new List<Point>();
            var first = corners.OrderBy(p => p.X + p.Y).First();
            corners.Remove(first);
            sortedCorners.Add(first);

            var second = corners.OrderBy(p => p.Y).First();
            corners.Remove(second);
            sortedCorners.Add(second);

            var third = corners.OrderBy(p => p.X).Last();
            corners.Remove(third);
            sortedCorners.Add(third);

            sortedCorners.Add(corners[0]);

            return sortedCorners;
        }
    }
}
