using System.Collections.Generic;
using System.Drawing;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal interface IPerspectiveTransformation
    {
        /// <summary>
        /// Строит и применяет матрицу устранения перспективных искажений
        /// </summary>
        /// <param name="image">Исходное изображение</param>
        /// <param name="corners">Координаты углов документа на изображении</param>
        /// <returns>Выпрямленное изображение</returns>
        KrecImage ApplyTransformMatrix(KrecImage image, List<Point> corners);
    }
}
