using System.Drawing;

namespace DocumentAdjuster.Services
{
    // https://habr.com/ru/post/278435/
    internal interface IBinarizationService
    {
        int[,] MakeBinarized(Bitmap image);
    }
}
