using System;
using System.Drawing;
using CommandLine;

namespace DocumentAdjuster
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    var document = new Bitmap(options.FileName);
                    Console.WriteLine(document.Width + " " + document.Height);
                    var documentAdjuster = new DocumentAdjuster();
                    documentAdjuster.SetDebugMode(options.Debug);

                    var result = documentAdjuster.Correct(document);
                    result.SaveToFile("result.bmp");
                });
        }
    }
}
