using System;
using System.Collections.Generic;
using System.Drawing;

namespace DocumentAdjuster.Services
{
    // https://habr.com/ru/post/102948/
    // http://robocraft.ru/blog/computervision/502.html
    internal interface IEquationOfLineService
    {
        Tuple<int, int>[] GetLines(List<Point> points, int count, int width, int height);
    }
}
