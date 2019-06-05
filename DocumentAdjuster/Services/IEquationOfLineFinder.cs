using System.Collections.Generic;
using System.Drawing;
using DocumentAdjuster.Models;

namespace DocumentAdjuster.Services
{

    internal interface IEquationOfLineFinder
    {
        /// <summary>
        /// Находит уравнения прямых по мн-ву точек
        /// </summary>
        /// <param name="points">Точки принадлежащие границам документа</param>
        /// <param name="count">Число возвращаемых уравнений прямых</param>
        /// <param name="width">Ширина изображения</param>
        /// <param name="height">Высота изображения</param>
        /// <returns>Уравнения прямых</returns>
        EquationOfLine[] GetLines(List<Point> points, int count, int width, int height);
    }
}
