using System.Collections.Generic;
using System.Drawing;
using DocumentAdjuster.Models;

namespace DocumentAdjuster.Services
{
    internal interface ICornerFinder
    {
        List<Point> FindCorner(EquationOfLine[] borders, int width, int height);
    }
}
