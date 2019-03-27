using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontur.Recognition.ImageCore;
using Kontur.Recognition.ImageDeskewer.ImageProcessing;

namespace Kontur.Recognition.ImageDeskewer.RectanglesProcessing
{
	/// <summary>
	/// Contains model of text in image
	/// </summary>
	class RectanglesProcessor
	{
		private const int minFontSizePt = 3;
		private const int maxFontSizePt = 150;
		private const double ptInInch = 1 / 72.0;
		private readonly HeightsPartition[] heightsPartitions;
		/// <summary>
		/// Maximal box height in pixels whith which box will be classified as a char
		/// </summary>
		private readonly int maxCharHeight;

		/// <summary>
		/// Boxes that bounds clusters of black points
		/// </summary>
		public Rectangle[] Boxes { get; private set; }
		/// <summary>
		/// Lines of chars
		/// </summary>
		public TextLine[] Lines { get; private set; }
		/// <summary>
		/// List of rectangles which contains too small chars in it
		/// </summary>
		public Rectangle[] BadChars { get; private set; }
		/// <summary>
		/// Is orientation detection was succeeded.
		/// </summary>
		public bool DetectionSucceeded { get; private set; }

		/// <summary>
		/// Maximal box height in pixels whith which box will be classified as a char
		/// </summary>
		public int MaxCharHeight
		{
			get { return maxCharHeight; }
		}

		struct HeightsPartition
		{
			public readonly double UpperBorder;
			public readonly double LowerBorder;
			public readonly int RealHeight;

			public HeightsPartition(double upperBorder, double lowerBorder, int realHeight)
				: this()
			{
				UpperBorder = upperBorder;
				LowerBorder = lowerBorder;
				RealHeight = realHeight;
			}
		}

		/// <summary>
		/// Initialize processor for grayscale image
		/// </summary>
		/// <param name="image">Grayscale image</param>
		public RectanglesProcessor(KrecImage image)
			: this(BitmapProcessor.BinarizeGrayscaleImage(image), image.VerticalResolution)
		{
			
		}

		/// <summary>
		/// Initialize processor for binary image
		/// </summary>
		/// <param name="binary">Binary image</param>
		/// <param name="verticalResolution">Image vertical resolution</param>
		public RectanglesProcessor(bool[,] binary, double verticalResolution)
			: this(new BoxParser(binary).Boxes, verticalResolution)
		{
			
		}

		/// <summary>
		/// Initialize processor for any image
		/// </summary>
		/// <param name="boxes">Boxes that bound text lines on image</param>
		/// <param name="verticalResolution">Image vertical resolution</param>
		public RectanglesProcessor(Rectangle[] boxes, double verticalResolution)
		{
			DetectionSucceeded = false;
			Boxes = boxes;
			
			// Пытаемся оценить ожидаемый размер символов на изображении
			var minCharHeight = (int) Math.Round(minFontSizePt * ptInInch * verticalResolution); //Первое приближение
			
			// Maximal box height in pixels whith which box will be classified as a char
			var maxCharHeightEstimation = (int) Math.Round(maxFontSizePt * ptInInch * verticalResolution); //Для ускорения вычислений, предполагаем, что не будет символов больше maxFontSizePt пунктов
			
			// Вычисление распределения (гистограммы) высот блоков, чтобы выделить на ней волны (сегменты похожих высот)
			var heightsDistribution = BuildHeightsDistribution(boxes, maxCharHeightEstimation);
			
			// Если исходное изображение было сформировано с неверным разрешением, то оценка выше может оказаться неверной
			// (не найдем символов нужного размера). В этом случае пробуем уменьшить ожидаемый размер символов, чтобы все же провести обнаружение угла
			// probabilityDistributionRatio - отношение числа прямоугольников по высоте больше минимальной ожидаемой высоты символа к
			// количеству прямоугольников ниже этой высоты. На реальных кейсах было подобрано число 0.02, ниже которого оценка получается неверной.
			// Возможно его нужно увеличить, но сейчас имеющихся данных для этого не достаточно.
			var probabilityDistributionRatio = GetProbabilityDistributionRatio(heightsDistribution, minCharHeight);
			if (probabilityDistributionRatio < 0.02)
			{
				minCharHeight = FixMinCharHeight(heightsDistribution, minCharHeight);
			}
			if (minCharHeight == 0)
			{
				// Текстовых блоков не обнаружено. Дальнейшее детектирование невозможно.
				Lines = new TextLine[0];
				return;
			}
			
			// Делим гистограмму высот на зоны, в каждой из которых сначала идет монотонный рост, затем монотонное уменьшение.
			heightsPartitions = GetBoxesHeightPartitioning(heightsDistribution, minCharHeight);
			
			minCharHeight = (int) Math.Ceiling(heightsPartitions[0].LowerBorder);
			maxCharHeight = GetMaxCharHeightEstimation(heightsDistribution, minCharHeight);
			
			var chars = Boxes.Where(x => x.Height >= minCharHeight && x.Height <= maxCharHeight).ToList();
			BadChars = Boxes.Where(x => x.Height < minCharHeight).ToArray();
			FindTextLines(chars);
			DetectionSucceeded = Lines.Length > 0;
		}

		private int GetMaxCharHeightEstimation(int[] heightsDistribution, int minCharHeight)
		{
			var result = 0;
			int minGoodCount = (int) (heightsDistribution.Skip(minCharHeight).Sum() * 0.018); //Средняя длина предложения - 55 символов. 1/55.
			for (int partNumber = heightsPartitions.Length - 1; partNumber >= 0; --partNumber)
			{
				var charsCount = 0;
				for (var charHeight = (int) heightsPartitions[partNumber].UpperBorder;
					charHeight >= heightsPartitions[partNumber].LowerBorder;
					--charHeight)
				{
					charsCount += heightsDistribution[charHeight];
				}
				if (charsCount >= minGoodCount)
				{
					result = (int) heightsPartitions[partNumber].UpperBorder;
					break;
				}
			}
			return result;
		}

		private int FixMinCharHeight(int[] heightsDistribution, int minCharHeight)
		{
			var maxBoxHeight = minCharHeight - 1;
			while (maxBoxHeight >= 0 && heightsDistribution[maxBoxHeight] == 0)
			{
				maxBoxHeight--;
			}
			
			if (maxBoxHeight > 0)
			{
				minCharHeight = maxBoxHeight - 1; // чтобы избежать ситуации, когда максимальная высота достигается лишь на одном блоке
				
				if (minCharHeight < 2)
				{
					minCharHeight = 2; // искусственное ограничение снизу
				}
			}
			
			if (maxBoxHeight <= 0)
			{
				return 0;
			}
			
			return minCharHeight;
		}

		/// <summary>
		/// Get ratio of heights which less and more than min char height
		/// </summary>
		/// <param name="heightsDistribution">Distribution of rectangles height</param>
		/// <param name="minCharHeight">Min char height</param>
		/// <returns></returns>
		private double GetProbabilityDistributionRatio(int[] heightsDistribution, int minCharHeight)
		{
			var sumHeightsLessThanMin = heightsDistribution.Take(minCharHeight).Sum();
			var sumHeightsMoreThanMin = heightsDistribution.Skip(minCharHeight).Sum();
			
			if (sumHeightsLessThanMin == 0) return 1;
			
			return (double) sumHeightsMoreThanMin / sumHeightsLessThanMin;
		}

		/// <summary>
		/// Find solid lines of chars
		/// </summary>
		/// <param name="chars"></param>
		private void FindTextLines(List<Rectangle> chars)
		{
			var lines = FindLines(chars);
			var linesWithoutPunctuation = AllocatePunctuation(lines);
			var dividedLines = DivideLinesBySpaces(linesWithoutPunctuation);
			Lines = GetWords(dividedLines).Where(line => (line.Chars.Length > 1 || line.Chars[0].Width / line.Chars[0].Height < 10)).ToArray(); //отсекает длинные горизонтальные линии
		}

		/// <summary>
		/// Find Boxes that bound chars in processed bitmap
		/// </summary>
		private Rectangle[] FindChars(int minCharHeight, int maxCharHeight)
		{
			return Boxes.Where(x => x.Height >= minCharHeight && x.Height <= maxCharHeight).ToArray();
		}

		/// <summary>
		/// Rotate boxes by Pi/2
		/// </summary>
		/// <param name="boxes">Rotated boxes</param>
		/// <param name="width">Width of image width boxes</param>
		/// <param name="height">Height of image width boxes</param>
		/// <returns>Rotated boxes</returns>
		public static Rectangle[] RotateBoxesBy90(Rectangle[] boxes, int width, int height)
		{
			return boxes.Select(box => new Rectangle(height - (box.Y + box.Height), box.X, box.Height, box.Width)).ToArray();
		}

		/// <summary>
		/// Splits heights to ranges by given histogram. Each range must comply with the property: at each range histogram is first monotonically increases, then monotonically decreases 
		/// (i.e. we detect waves in histogram and break the whole heigths range at minimal points of waves)
		/// </summary>
		/// <param name="bars">Distribution of boxes heights (the i-th element of the array holds the number of boxes of the height i)</param>
		/// <param name="minCharHeight">The minimal size of character to take into account</param>
		/// <returns></returns>
		private static HeightsPartition[] GetBoxesHeightPartitioning(int[] bars, int minCharHeight)
		{
			int skipCount = minCharHeight;
			bars = bars.Skip(skipCount).ToArray();
			int len = bars.Length;
			var charsHeights = new List<HeightsPartition>();
			var i = 0;
			while (i < len - 2 && bars[i] == 0)
				++i;
			
			while (i < len - 2)
			{
				double leftBorder;
				if (i == 0 || (bars[i - 1] == 0))
					leftBorder = i + skipCount;
				else
					leftBorder =
						((double) (i + 1) * bars[i + 1] + (i - 1) * bars[i - 1]) / (bars[i + 1] + bars[i - 1]) +
						skipCount;
				
				
				while (i < len - 2 && bars[i] <= bars[i + 1])
					++i;
				int top = i + skipCount;
				
				while (i < len - 2 && bars[i] > bars[i + 1])
					++i;
				double rightBorder;
				if ((bars[i + 1] + bars[i - 1]) == 0)
					rightBorder = i + skipCount;
				else
					rightBorder =
						((double) (i + 1) * bars[i + 1] + (i - 1) * bars[i - 1]) / (bars[i + 1] + bars[i - 1]) +
						skipCount;
				
				charsHeights.Add(new HeightsPartition(rightBorder, leftBorder, top));
				
				while (i < len - 2 && bars[i] >= bars[i + 1]) //Поиск левой границы
					++i;
				if (i == len - 2)
					break;
			}
			return charsHeights.ToArray();
		}

		/// <summary>
		/// Find lines of chars, locating on one level, that doesn't contain spaces bigger, then neighbour chars height
		/// </summary>
		/// <param name="chars">Chars</param>
		/// <returns>Text lines which have been found</returns>
		private static IEnumerable<TextLine> FindLines(List<Rectangle> chars)
		{
			// TODO: reimplement to avoid memory movements
			chars = chars.OrderBy(c => c.X).ToList();
			var words = new List<TextLine>();
			while (chars.Count != 0)
			{
				var currentWord = new TextLine(chars[0]);
				chars.RemoveAt(0);
				words.Add(currentWord);
				
				var currentRect = currentWord.BoundingRect;
				for (int j = 0; j < chars.Count; ++j)
				{
					var currentChar = chars[j];
					int center = (currentChar.Bottom + currentChar.Top) / 2;
					
					// The character is above or below the current word, so it must go to another line (so ignore it for a while)
					if (center <= currentRect.Top || center >= currentRect.Bottom)
					{
						continue;
					}
					
					// The character stands too far from the word, so it should start a new word
					if (Math.Abs(currentChar.Left - currentRect.Right) >=
						Math.Max(currentChar.Height, currentRect.Height))
					{
						break;
					}
					
					currentWord.AddChar(currentChar);
					currentRect = currentWord.BoundingRect;
					chars.RemoveAt(j);
					--j;
				}
			}
			
			return words;
		}

		/// <summary>
		/// Divide text lines to lines that doesn't contains spaces bigger then average line height
		/// </summary>
		/// <param name="lines">Text lines to diveide</param>
		/// <returns>Divided text lines</returns>
		private IEnumerable<TextLine> DivideLinesBySpaces(IEnumerable<TextLine> lines)
		{
			var result = new List<TextLine>();
			foreach (var line in lines)
			{
				int realHeight = GetRealHeight(line);
				var newChars = line.Chars.ToList();
				for (int i = 1; i < newChars.Count; ++i)
				{
					if (newChars[i].Left - newChars[i - 1].Right > realHeight)
					{
						result.Add(new TextLine(newChars.GetRange(0, i).ToArray()));
						newChars.RemoveRange(0, i);
						i = 1;
					}
				}
				result.Add(new TextLine(newChars.ToArray()));
			}
			return result;
		}

		/// <summary>
		/// Allocate punctuation from text lines in other lines
		/// </summary>
		/// <param name="lines">Text lines to allocate punctuation</param>
		/// <returns>Text lines with allocated punctuation</returns>
		private static IEnumerable<TextLine> AllocatePunctuation(IEnumerable<TextLine> lines)
		{
			var result = new List<TextLine>();
			foreach (var line in lines)
			{
				var maxHeight = line.Chars.Max(x => x.Height);
				var goodChars = line.Chars.Where(x => maxHeight / (double) x.Height <= 1.7).ToArray();
				result.Add(new TextLine(goodChars));

				var punctuation = line.Chars.Where(x => maxHeight / (double) x.Height > 1.7).ToList();
				if (punctuation.Count == 0)
					continue;
				var punctuationLines = FindLines(punctuation).Where(l => l.Chars.Count() > 1);
				result.AddRange(punctuationLines);
			}
			return result;
		}

		/// <summary>
		/// Divide text lines by spaces, that bigger then other spaces in line
		/// </summary>
		/// <param name="lines">Text lines to diveide</param>
		/// <returns>Divided text lines</returns>
		private static IEnumerable<TextLine> GetWords(IEnumerable<TextLine> lines)
		{
			return lines.SelectMany(GetWords).ToArray();
		}

		/// <summary>
		/// Splits text line by spaces which are bigger than other spaces in line
		/// </summary>
		/// <param name="line">Text line to split</param>
		/// <returns>Split text line</returns>
		private static TextLine[] GetWords(TextLine line)
		{
			double average = 0;
			
			// Number of intercharacter intervals
			int spacesCount = line.Chars.Length - 1;
			
			if (spacesCount == 0)
			{
				return new[] {line};
			}
			
			var result = new List<TextLine>();
			
			int goodSpaceCount = 0;
			var spaces = new int[spacesCount];
			for (int i = 0; i < spacesCount; ++i)
			{
				int space = line.Chars[i + 1].Left - line.Chars[i].Right;
				if (space > 0)
				{
					goodSpaceCount++;
					average += space;
					spaces[i] = space;
				}
			}
			average /= goodSpaceCount;
			
			int startIndex = 0;
			for (int i = 0; i < spacesCount; ++i)
			{
				if (spaces[i] > 1.5 * average) //Иначе каждое слово будет разделено
				{
					result.Add(new TextLine(line.Chars.Skip(startIndex).Take(i - startIndex + 1).ToArray()));
					startIndex = i + 1;
				}
			}
			result.Add(new TextLine(line.Chars.Skip(startIndex).Take(spacesCount - startIndex + 1).ToArray()));

			return result.ToArray();
		}

		/// <summary>
		/// Classify words height
		/// </summary>
		/// <param name="word">Words to classify</param>
		/// <returns>Real height</returns>
		private int GetRealHeight(TextLine word)
		{
			var avg = word.Chars.Average(x => x.Height);
			int lineHeight = (int) avg;
			foreach (var partition in heightsPartitions)
			{
				if (avg <= partition.UpperBorder && avg >= partition.LowerBorder)
				{
					lineHeight = partition.RealHeight;
					break;
				}
			}
			return lineHeight;
		}

		/// <summary>
		/// Builds distribution of heights for given collection of boxes. Only those boxes which fit specified restrictions on minimal height (inclusive) and maximal height (exclusive) 
		/// are included into distribution
		/// </summary>
		/// <param name="boxes">Boxes, bounding black places</param>
		/// <param name="maxCharHeight">The maximal height to account (exclusive)</param>
		/// <returns></returns>
		private static int[] BuildHeightsDistribution(IEnumerable<Rectangle> boxes, int maxCharHeight)
		{
			var result = new int[maxCharHeight];
			foreach (var box in boxes)
			{
				if (box.Height < maxCharHeight)
				{
					result[box.Height]++;
				}
			}
			return result;
		}
	}
}
