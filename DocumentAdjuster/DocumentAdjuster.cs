using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using DocumentAdjuster.Models;
using DocumentAdjuster.Services;
using Kontur.Recognition.ImageBinarizer;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster
{
    // https://habr.com/ru/company/abbyy/blog/312570/
    // https://habr.com/ru/company/abbyy/blog/200448/
    internal class DocumentAdjuster
    {
        private readonly IBorderSearcher borderSearcher;
        private readonly IEquationOfLineFinder equationOfLineFinder;
        private readonly IMedianFilter medianFilter;
        private readonly ICornerFinder cornerFinder;
        private readonly IPerspectiveTransformation perspectiveTransformation;
        private bool debug;

        private double dx;
        private double dy;

        public DocumentAdjuster()
        {
            borderSearcher = new BorderSearcher();
            equationOfLineFinder = new EquationOfLineFinder();
            medianFilter = new MedianFilter();
            cornerFinder = new CornerFinder();
            perspectiveTransformation = new PerspectiveTransformation();
        }

        public KrecImage Correct(Bitmap document)
        {
            var normalizeDocument = ReduceImage(document);
            var width = normalizeDocument.Width;
            var height = normalizeDocument.Height;

            Save(normalizeDocument, "smallDocument");
            var sw = Stopwatch.StartNew();
            var binaryDocument = normalizeDocument.Binarize();
            sw.Stop();
            Console.WriteLine($"Binarization, time: {sw.ElapsedMilliseconds} ms");
            Save(binaryDocument, "binaryResult");

            sw = Stopwatch.StartNew();
            var filterDocument = medianFilter.Apply(binaryDocument, 3);
            filterDocument = medianFilter.Apply(filterDocument, 5);
            sw.Stop();
            Console.WriteLine($"Apply median filter, time: {sw.ElapsedMilliseconds} ms");
            Save(filterDocument, "filterResult");

            sw = Stopwatch.StartNew();
            var sobelX = new[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            var sobelY = new[,] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };
            var documentWithBorders = borderSearcher.Search(filterDocument, sobelX, sobelY);
            sw.Stop();
            Console.WriteLine($"Get points of borders, time: {sw.ElapsedMilliseconds} ms");
            Save(documentWithBorders, "bordersResult");

            sw = Stopwatch.StartNew();
            var lines = equationOfLineFinder.GetLines(borderSearcher.GetPoints(), 4, width, height);
            sw.Stop();
            Console.WriteLine($"Find equations of borders: {sw.ElapsedMilliseconds} ms");
            SaveBorders(lines, width, height);

            sw = Stopwatch.StartNew();
            var corners = cornerFinder.FindCorner(lines, width, height);
            corners = corners.Select(c => new Point((int)Math.Round(c.X * dx), (int)Math.Round(c.Y * dy))).ToList();
            sw.Stop();
            Console.WriteLine($"Find corners: {sw.ElapsedMilliseconds} ms");

            sw = Stopwatch.StartNew();
            var correctResult = perspectiveTransformation.ApplyTransformMatrix(KrecImage.FromBitmap(document), corners);
            sw.Stop();
            Console.WriteLine($"Apply transform matrix and smoothing filter: {sw.ElapsedMilliseconds} ms");
            return correctResult;
        }

        public void SetDebugMode(bool debugMode) => debug = debugMode;

        private KrecImage ReduceImage(Bitmap document)
        {
            var width = 300;
            var height = width * ((double) document.Height / document.Width);
            dx = (double)document.Width / width;
            dy = document.Height / height;
            return KrecImage.FromBitmap(new Bitmap(document, width, (int) height));
        }

        private void Save(KrecImage image, string name)
        {
            if (!debug)
                return;
            image.SaveToFile($"{name}.bmp");
        }

        private void SaveBorders(EquationOfLine[] lines, int width, int height)
        {
            if (!debug)
                return;
            
            var result = new Bitmap(width, height);
            foreach (var line in lines)
            {
                var theta = line.Angle * Math.PI / 180.0;
                var r = line.Radius;
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        if ((int)Math.Round(y * Math.Sin(theta) + x * Math.Cos(theta)) == r)
                            result.SetPixel(x, y, Color.White);
                    }
                }
            }
            result.Save("borders.bmp", ImageFormat.Bmp);
        }

    }
}
