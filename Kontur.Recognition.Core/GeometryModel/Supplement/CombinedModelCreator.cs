using System;
using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.Utils;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel.Supplement
{
	/// <summary>
	/// Класс строит объединение двух моделей как объединение всех слов (слова считаются одинаковыми, если совпадают как текст и находятся примерно в одном месте).
	/// </summary>
    public class CombinedModelCreator
    {
		private class GMWordComparerWithInaccuracy : IEqualityComparer<GMWord>
		{
			public static readonly GMWordComparerWithInaccuracy Instance = new GMWordComparerWithInaccuracy();

			public bool Equals(GMWord x, GMWord y)
			{
				var boxX = x.BoundingBox;
				var boxY = y.BoundingBox;
				return (String.Compare(x.Text, y.Text, StringComparison.Ordinal) == 0 &&
					Math.Abs(boxX.XMax - boxY.XMax) < 25 && Math.Abs(boxX.XMin - boxY.XMin) < 25 && Math.Abs(boxX.YMax - boxY.YMax) < 25 && Math.Abs(boxX.YMin - boxY.YMin) < 25);
			}

			public int GetHashCode(GMWord obj)
			{
				return obj.Text.GetHashCode();
			}
		}
		
		private readonly int xEpsilon;
        private readonly int yEpsilon;

        public CombinedModelCreator(int xEpsilon, int yEpsilon)
        {
            this.xEpsilon = xEpsilon;
            this.yEpsilon = yEpsilon;
        }

		public HashSet<GMWord> GetSameWords([NotNull] TextGeometryModel fineReaderModel, [NotNull] TextGeometryModel tesseractModel)
		{
			const double targetResolution = 150;
			var scaledFrModel = fineReaderModel.ScaleModel(targetResolution / fineReaderModel.GridUnit.Divisor);
			var scaledTesModel = tesseractModel.ScaleModel(targetResolution / tesseractModel.GridUnit.Divisor);
            var tesseractCache = new LinesHashInGeometryModel(scaledTesModel.Words(), yEpsilon, xEpsilon, scaledTesModel.PageBox);
            var allSameWords = scaledFrModel.Words().Where(x => tesseractCache.Contains(x, String.Compare));
            return new HashSet<GMWord>(allSameWords);
        }

        public TextGeometryModel GetSampleModel(TextGeometryModel fineReaderModel, TextGeometryModel tesseractModel, [NotNull] GridUnit gridUnit)
        {
            var scaledFrModel = fineReaderModel.ScaleModel(gridUnit.Divisor / (double)fineReaderModel.GridUnit.Divisor);
			var scaledTesModel = tesseractModel.ScaleModel(gridUnit.Divisor / (double)tesseractModel.GridUnit.Divisor);
            var tesseractCache = new LinesHashInGeometryModel(scaledTesModel.Words(), yEpsilon, xEpsilon, scaledTesModel.PageBox);
            var sampleModel = new TextGeometryModel(scaledFrModel.PageBox, gridUnit);
            var sameWords = new List<GMWord>(EnumerateWordsFromFineReader(scaledFrModel, sampleModel, tesseractCache));
            AddWordsFromTesseract(scaledTesModel, sampleModel, sameWords);
            return sampleModel;
        }

        private static void AddWordsFromTesseract(TextGeometryModel tesseractModel, TextGeometryModel sampleModel, List<GMWord> set)
        {
            var box = new BoundingBox(tesseractModel.PageBox.XMin, tesseractModel.PageBox.YMin,
                tesseractModel.PageBox.XMax, tesseractModel.PageBox.YMax);
            var block = sampleModel.AddTextBlock(box);
            var paragraph = block.AddParagraph(box);
            var line = paragraph.AddLine(box);
            foreach (var word in tesseractModel.Words())
            {
                //compare with special comparator, which compare boundingboxes with inaccuracy
                if (!set.Contains(word, GMWordComparerWithInaccuracy.Instance))
                {
                    var wordBox = new BoundingBox(word.BoundingBox.XMin, word.BoundingBox.YMin, word.BoundingBox.XMax, word.BoundingBox.YMax);
                    line.AddWord(new GMWord(wordBox, word.Text, 50));
                }
            }
        }

        //return HashSet coincidered words
        private static IEnumerable<GMWord> EnumerateWordsFromFineReader(TextGeometryModel fineReaderModel, TextGeometryModel sampleModel,
            LinesHashInGeometryModel tesseractCache)
        {
            foreach (var block in fineReaderModel.TextBlocks())
            {
                var blockBox = block.BoundingBox;
                var thisBlock =
                    sampleModel.AddTextBlock(new BoundingBox(blockBox.XMin, blockBox.YMin, blockBox.XMax, blockBox.YMax));
                foreach (var paragraph in block.Paragraphs())
                {
                    var paragraphBox = paragraph.BoundingBox;
                    var thisParagraph =
                        thisBlock.AddParagraph(new BoundingBox(paragraphBox.XMin, paragraphBox.YMin, paragraphBox.XMax,
                            paragraphBox.YMax));
                    foreach (var line in paragraph.Lines())
                    {
                        var lineBox = line.BoundingBox;
                        var thisLine =
                            thisParagraph.AddLine(new BoundingBox(lineBox.XMin, lineBox.YMin, lineBox.XMax, lineBox.YMax));
                        foreach (var word in line.Words())
                        {
                            var wordBox = word.BoundingBox;
                            var accuracy = 50;
                            if (tesseractCache.Contains(word, String.Compare))
                            {
                                yield return word;
                                accuracy = 100;
                            }
                            var thisWord = new GMWord(wordBox, word.Text, accuracy);
                            thisLine.AddWord(thisWord);
                        }
                    }
                }
                foreach (var word in block.StandaloneWords())
                {
                    var wordBox = word.BoundingBox;
                    var accuracy = 50;
                    if (tesseractCache.Contains(word, String.Compare))
                    {
                        yield return word;
                        accuracy = 100;
                    }
                    var thisWord = new GMWord(wordBox, word.Text, accuracy);
                    thisBlock.AddStandaloneWord(thisWord);
                }
            }
        }
    }
}
