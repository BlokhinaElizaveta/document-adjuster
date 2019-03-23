using System;
using System.Collections.Generic;
using System.Drawing;

namespace DocumentAdjuster.Services
{
    internal interface ICornerFinder
    {
        List<Point> FindCorner(Tuple<int, int>[] borders, int width, int height);
    }
}
