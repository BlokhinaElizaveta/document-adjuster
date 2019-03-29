using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using DocumentAdjuster.Services;
using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster
{
    internal class DocumentAdjuster : IDocumentAdjuster
    {
        private readonly IBinarizationService binarizationService;
        private readonly IBorderSearchService borderSearchService;
        private readonly IEquationOfLineService equationOfLineService;
        private readonly IMedianFilterService medianFilterService;
        private readonly ICornerFinder cornerFinder;
        private readonly IProjectiveTransformationService projectiveTransformation;
        private bool debug;

        private double dx;
        private double dy;

        public DocumentAdjuster(IBinarizationService binarizationService,
            IBorderSearchService borderSearchService,
            IEquationOfLineService equationOfLineService,
            IMedianFilterService medianFilterService,
            ICornerFinder cornerFinder,
            IProjectiveTransformationService projectiveTransformation
        )
        {
            this.binarizationService = binarizationService;
            this.borderSearchService = borderSearchService;
            this.equationOfLineService = equationOfLineService;
            this.medianFilterService = medianFilterService;
            this.cornerFinder = cornerFinder;
            this.projectiveTransformation = projectiveTransformation;
        }

        public KrecImage Correct(Bitmap document)
        {
            var normalizeDocument = ReduceImage(document);
            var width = normalizeDocument.Width;
            var height = normalizeDocument.Height;

            var sw = Stopwatch.StartNew();
            var binaryDocument = binarizationService.MakeBinarized(normalizeDocument);
            sw.Stop();
            Console.WriteLine($"Binarization, time: {sw.ElapsedMilliseconds} ms");
            Save(binaryDocument, "binaryResult");

            sw = Stopwatch.StartNew();
            var filterDocument = medianFilterService.Apply(binaryDocument, 1);
            filterDocument = medianFilterService.Apply(filterDocument, 5);
            sw.Stop();
            Console.WriteLine($"Apply median filter, time: {sw.ElapsedMilliseconds} ms");
            Save(filterDocument, "filterResult");

            sw = Stopwatch.StartNew();
            var sobelX = new[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            var sobelY = new[,] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };
            var documentWithBorders = borderSearchService.Search(filterDocument, sobelX, sobelY);
            sw.Stop();
            Console.WriteLine($"Get points of borders, time: {sw.ElapsedMilliseconds} ms");
            Save(documentWithBorders, "bordersResult");

            sw = Stopwatch.StartNew();
            var lines = equationOfLineService.GetLines(borderSearchService.GetPoints(), 4, width, height);
            sw.Stop();
            Console.WriteLine($"Find equations of borders: {sw.ElapsedMilliseconds} ms");
            SaveBorders(lines, width, height);

            sw = Stopwatch.StartNew();
            var corners = cornerFinder.FindCorner(lines, width, height);
            corners = corners.Select(c => new Point((int)Math.Round(c.X * dx), (int)Math.Round(c.Y * dy))).ToList();
            sw.Stop();
            Console.WriteLine($"Find corners: {sw.ElapsedMilliseconds} ms");

            sw = Stopwatch.StartNew();
            var correctResult = projectiveTransformation.ApplyTransformMatrix(KrecImage.FromBitmap(document), SortCorners(corners));
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

        private static List<Point> SortCorners(IList<Point> corners)
        {
            var sortedCorners = new List<Point>();
            var first = corners.OrderBy(p => p.X + p.Y).First();
            corners.Remove(first);
            sortedCorners.Add(first);

            var second = corners.OrderBy(p => p.Y).First();
            corners.Remove(second);
            sortedCorners.Add(second);

            var third = corners.OrderBy(p => p.X).Last();
            corners.Remove(third);
            sortedCorners.Add(third);

            sortedCorners.Add(corners[0]);

            return sortedCorners;
        }

        private void Save(KrecImage image, string name)
        {
            if (!debug)
                return;
            image.SaveToFile($"{name}.bmp");
        }

        private void SaveBorders(Tuple<int, int>[] lines, int width, int height)
        {
            if (!debug)
                return;
            
            var result = new Bitmap(width, height);
            foreach (var line in lines)
            {
                var theta = line.Item1 * Math.PI / 180.0;
                var r = line.Item2;
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
