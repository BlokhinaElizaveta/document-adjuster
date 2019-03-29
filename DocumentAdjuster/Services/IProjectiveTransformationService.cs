using System.Collections.Generic;
using System.Drawing;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal interface IProjectiveTransformationService
    {
        KrecImage ApplyTransformMatrix(KrecImage image, List<Point> corners);
    }
}
