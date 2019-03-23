using System.Collections.Generic;
using System.Drawing;

namespace DocumentAdjuster.Services
{
    // https://habr.com/ru/post/114452/
    internal interface IBorderSearchService
    {
        int[,] Search(int[,] image, int[,] maskX, int[,] maskY);
        List<Point> GetPoints();
    }
}