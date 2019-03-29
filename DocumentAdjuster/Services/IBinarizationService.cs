using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    // https://habr.com/ru/post/278435/
    internal interface IBinarizationService
    {
        KrecImage MakeBinarized(KrecImage image);
    }
}
