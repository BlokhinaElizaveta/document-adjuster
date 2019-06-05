using System.Collections.Generic;
using System.Drawing;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal interface IBorderSearcher
    {
        /// <summary>
        /// Находит границы документа на изображении
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maskX">Матрица 3*3 - ядро свёртки по x</param>
        /// <param name="maskY">Матрица 3*3 - ядро свёртки по y</param>
        /// <returns>Изображение с нарисованными границами</returns>
        KrecImage Search(KrecImage image, int[,] maskX, int[,] maskY);

        /// <returns>Точки принадлежащие границам</returns>
        List<Point> GetPoints();
    }
}