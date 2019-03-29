using System.Collections.Generic;
using System.Drawing;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    // https://habr.com/ru/post/114452/
    internal interface IBorderSearchService
    {
        KrecImage Search(KrecImage image, int[,] maskX, int[,] maskY);
        List<Point> GetPoints();
    }
}