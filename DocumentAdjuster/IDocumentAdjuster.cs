using System.Drawing;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster
{
    // https://habr.com/ru/company/abbyy/blog/312570/
    // https://habr.com/ru/company/abbyy/blog/200448/
    internal interface IDocumentAdjuster
    {
        KrecImage Correct(Bitmap documentWithBorder);
        void SetDebugMode(bool debug);
    }
}
