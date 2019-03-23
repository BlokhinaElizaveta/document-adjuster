using System.Drawing;

namespace DocumentAdjuster
{
    // https://habr.com/ru/company/abbyy/blog/312570/
    // https://habr.com/ru/company/abbyy/blog/200448/
    internal interface IDocumentAdjuster
    {
        Bitmap Correct(Bitmap documentWithBorder);
        void SetDebugMode(bool debug);
    }
}
