using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal interface IMedianFilterService
    {
        KrecImage Apply(KrecImage image, int radius);
    }
}
