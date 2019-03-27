using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontur.Recognition.ImageCore;
using Kontur.Recognition.ImageDeskewer.ImageProcessing;
using Kontur.Recognition.ImageDeskewer.RectanglesProcessing;

namespace Kontur.Recognition.ImageDeskewer
{
	/// <summary>
	/// Determine orientation of image (result - angle to right orientation, multiples Pi/2, from -Pi/2 to Pi)
	/// </summary>
	static class OrientationDeterminer
	{
		/// <summary>
		/// Get angle to right image orientation (Multiples Pi/2, from -Pi/2 to Pi)
		/// </summary>
		/// <param name="image">Deskewed mage which nedd to be oriented</param>
		/// <returns>Angle to right orientation</returns>
		public static double GetAngleToRightOrientation(KrecImage image)
		{
			var binary = BitmapProcessor.BinarizeGrayscaleImage(image);
			
			var result90 = Get90AngleToRightOrientation(binary, image.HorizontalResolution, image.VerticalResolution);
			
			bool isCorrect90Oriented = result90.Item1;
			var lines = result90.Item2.Lines;
			
			// Not enough information to perform calculations of rotation angle
			// All lines contains less than one char inside
			if (lines.All(line => line.Chars.Length <= 1))
			{
				return 0;
			}
			
			if (!isCorrect90Oriented)
			{
				binary = RotateMatrixBy90(binary);
			}
			
			CleanUpBinaryFromNoise(binary, result90.Item2.BadChars);
			
			var linesSum = GetBluredBoundingRectsSums(lines.Select(line => line.BoundingRect), binary);
			
			double orientSum = 0;
			for (var i = 0; i < linesSum.Count; ++i)
			{
				orientSum += GetLineOrientation(lines[i].BoundingRect, binary, linesSum[i], result90.Item2.MaxCharHeight);
			}
			
			double expectation = orientSum / linesSum.Count;
			
			bool isCorrect180Oriented = (Math.Abs(expectation) < 0.011) || orientSum >= 0; //при мат. ожидании меньше 0.011 результаты данной оценки недостоверны, а статистически вероятнее всего изображение ориентировано правильно
			if (!isCorrect180Oriented && !isCorrect90Oriented)
				return -Math.PI / 2;
			if (!isCorrect180Oriented)
				return Math.PI;
			if (!isCorrect90Oriented)
				return Math.PI / 2;
			return 0;
		}

		/// <summary>
		/// Make all pixels white inside rectangles which contains only noise
		/// </summary>
		/// <param name="binary">Binary image</param>
		/// <param name="rectanglesToClean">Array of rectangles which contains only noise</param>
		private static void CleanUpBinaryFromNoise(bool[,] binary, Rectangle[] rectanglesToClean)
		{
			foreach (var rectangle in rectanglesToClean)
			{
				var widthLimit = rectangle.X + rectangle.Width;
				var heightLimit = rectangle.Y + rectangle.Height;
				for (var i = rectangle.X; i < widthLimit; i++)
				{
					for (var j = rectangle.Y; j < heightLimit; j++)
					{
						binary[i, j] = false;
					}
				}
			}
		}

		/// <summary>
		/// Determines if the image needs to be rotated by Pi/2 and finds a correctly oriented RectanglesProcessor
		/// </summary>
		/// <param name="binary">Binary image</param>
		/// <param name="horizontalResolution">Image horizontal resolution</param>
		/// <param name="verticalResolution">Image horizontal resolution</param>
		/// <returns>(Is image correctly oriented, Rectangle processor)</returns>
		private static Tuple<bool, RectanglesProcessor> Get90AngleToRightOrientation(bool[,] binary, double horizontalResolution, double verticalResolution)
		{
			var normRectProcessor = new RectanglesProcessor(binary, horizontalResolution);

			var width = binary.GetUpperBound(0) + 1;
			var height = binary.GetUpperBound(1) + 1;
			var rotRectProcessor = new RectanglesProcessor(RectanglesProcessor.RotateBoxesBy90(normRectProcessor.Boxes, width, height), verticalResolution);
			
			var orientationCorrect = true;
			if (rotRectProcessor.DetectionSucceeded)
			{
				if (normRectProcessor.DetectionSucceeded)
				{
					var normLines = normRectProcessor.Lines;
					var rotLines = rotRectProcessor.Lines;
					
					var l1 = normLines.Average(x => x.Chars.Length);
					var l2 = rotLines.Average(x => x.Chars.Length);
					
					if (Math.Abs(l1 - l2) < 0.01)
					{
						l1 = normLines.Average(x => x.BoundingRect.Width);
						l2 = rotLines.Average(x => x.BoundingRect.Width);
					}
					orientationCorrect = l1 / l2 >= 1;
				}
				else
				{
					orientationCorrect = false;
				}
			}
			
			return orientationCorrect ? new Tuple<bool, RectanglesProcessor>(true, normRectProcessor) : new Tuple<bool, RectanglesProcessor>(false, rotRectProcessor);
		}

		/// <summary>
		/// Get line orientation based on histogram of row sums in bounding rectangle
		/// </summary>
		/// <param name="boundingRect">Bounding rectangle for text line</param>
		/// <param name="binaryImage">Binary image which contains this text line</param>
		/// <param name="rowSumsHist">Histogramm of 1 pixel rows inside text line</param>
		/// <param name="maxCharHeight">Maximal char width in this image</param>
		/// <returns>Scores of line (>0 - right oriented)</returns>
		private static double GetLineOrientation(Rectangle boundingRect, bool[,] binaryImage, List<int> rowSumsHist, int maxCharHeight)
		{
			int maxSum = rowSumsHist.Max();
			if (maxSum != boundingRect.Width)
				return 0;
			
			int firstIndex = rowSumsHist.FindIndex(x => x == maxSum);
			int lastIndex = rowSumsHist.FindLastIndex(x => x == maxSum);
			
			if (lastIndex - firstIndex + 1 < boundingRect.Height / 2 || boundingRect.Height * 3 > boundingRect.Width || rowSumsHist.GetRange(firstIndex, lastIndex - firstIndex).Any(sum => sum != maxSum))
				return 0;
			
			var upperX = new int[boundingRect.Width];
			var lowerX = new int[boundingRect.Width];
			
			//Finding sum of offsets of black points above firstIndex and below lastIndex
			for (var x = boundingRect.Left; x < boundingRect.Right; ++x)
			{
				for (var rowNumber = 0; rowNumber < firstIndex; ++rowNumber)
				{
					if (binaryImage[x, boundingRect.Top + rowNumber])
					{
						upperX[x - boundingRect.Left] += firstIndex - rowNumber;
					}
				}
				for (var rowNumber = boundingRect.Height - 1; rowNumber > lastIndex; --rowNumber)
				{
					if (binaryImage[x, boundingRect.Top + rowNumber])
					{
						lowerX[x - boundingRect.Left] += rowNumber - lastIndex;
					}
				}
			}
			
			if (lowerX.Count(x => x > 0) > boundingRect.Width / 2 || upperX.Count(x => x > 0) > boundingRect.Width / 2) //TODO направлять boundingRect на разбиение
				return 0;
			int upperCount = upperX.Where((x, i) => i < maxCharHeight).Sum();
			int lowerCount = lowerX.Where((x, i) => i > boundingRect.Width - maxCharHeight).Sum();
			
			return (upperCount - lowerCount) / (double) boundingRect.Height;
		}

		/// <summary>
		/// Blur text inside bounding rects on binary image by horizontal, and finds sums for each row
		/// </summary>
		/// <param name="boundingRects">Rects which bound text lines</param>
		/// <param name="binaryImage">Binary image</param>
		/// <returns>List of bar graphs by rows sums </returns>
		private static List<List<int>> GetBluredBoundingRectsSums(IEnumerable<Rectangle> boundingRects, bool[,] binaryImage)
		{
			return boundingRects.Select(mbr => GetBluredByBoundingRectBinarySums(mbr, binaryImage)).ToList();
		}

		/// <summary>
		/// Blur text inside bounding rect on binary image by horizontal, and finds sums for each row
		/// </summary>
		/// <param name="boundingRect">Rect which bound text line</param>
		/// <param name="binary">Binary image</param>
		/// <returns>Bar graph by rows sums</returns>
		private static List<int> GetBluredByBoundingRectBinarySums(Rectangle boundingRect, bool[,] binary)
		{
			int dist = 2 * boundingRect.Height;
			var sums = new List<int>();
			for (int y = boundingRect.Top; y < boundingRect.Bottom; y++)
			{
				var sum = 0;
				int lastBlackX = boundingRect.Left - 1;
				for (var x = boundingRect.Left; x < boundingRect.Right; ++x)
				{
					if (binary[x, y])
					{
						if (x - lastBlackX < dist)
						{
							sum += x - lastBlackX;
						}
						else
							++sum;
						
						lastBlackX = x;
					}
				}
				if (boundingRect.Right - lastBlackX < dist)
				{
					sum += boundingRect.Right - lastBlackX - 1;
				}
				sums.Add(sum);
			}
			
			return sums;
		}
		/// <summary>
		/// Rotate matrix on pi/2 angle
		/// </summary>
		/// <param name="matrix">Matrix of boolean</param>
		/// <returns></returns>
		private static bool[,] RotateMatrixBy90(bool[,] matrix)
		{
			int width = matrix.GetUpperBound(0) + 1, height = matrix.GetUpperBound(1) + 1;
			var result = new bool[height, width];
			
			for (var x = 0; x < width; ++x)
			{
				for (var y = 0; y < height; ++y)
				{
					result[height - y - 1, x] = matrix[x, y];
				}
			}
			return result;
		}
	}
}