using System.Collections.Generic;
using System.Drawing;

namespace DocumentAdjuster.Services
{
    internal interface IProjectiveTransformationService
    {
        Bitmap ApplyTransformMatrix(Bitmap image, List<Point> corners);
    }
}
