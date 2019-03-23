
namespace DocumentAdjuster.Services
{
    internal interface IMedianFilterService
    {
        int[,] Apply(int[,] image, int radius);
    }
}
