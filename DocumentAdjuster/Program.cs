using System;
using System.Drawing;
using System.Drawing.Imaging;
using CommandLine;
using Ninject;

namespace DocumentAdjuster
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    var container = ContainerBuilder.Build();
                    var document = new Bitmap(options.FileName);
                    Console.WriteLine(document.Width + " " + document.Height);
                    var documentAdjuster = container.Get<IDocumentAdjuster>();
                    documentAdjuster.SetDebugMode(options.Debug);

                    var result = documentAdjuster.Correct(document);
                    result.Save("result.bmp", ImageFormat.Bmp);
                });
        }
    }
}
