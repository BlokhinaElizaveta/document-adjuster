using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Kontur.Recognition.ImageCore;

namespace Kontur.Recognition.ImageDeskewer
{
	/// <summary>
	/// Find skew angle of image (from -Pi/2 to Pi/2)
	/// </summary>
	class SkewAngleDeterminer
	{
		/// <summary>
		/// Calculate deskew angle of bitmap
		/// </summary>
		/// <param name="bmp">Skewed bitmap</param>
		/// <returns>Skew angle</returns>
		public static double CalculateAngle(Bitmap bmp)
		{
			var sourceImage = KrecImage.FromBitmap(bmp);
			var grayscaledImage = sourceImage.ToGrayscaled();
			return CalculateAngle(grayscaledImage);
		}

		/// <summary>
		/// Calculate angle for grayscale image
		/// </summary>
		/// <param name="grayImage">Image in grayscale</param>
		/// <returns>Skew angle</returns>
		public static double CalculateAngle(KrecImage grayImage)
		{
			const double maxError = 0.01;
			const int brightestPointsCount = 3000;
			
			var brightestPoints = GetBrightestPoints(grayImage, brightestPointsCount);
			double angle = FindAngleByWideBarGraph(maxError, brightestPoints, grayImage.Width);
			
			return CutAngle(angle); 
		}

		/// <summary>
		/// Return list of the brightest points after Fourier transform
		/// </summary>
		/// <param name="grayImage">Image to transform</param>
		/// <param name="pointsCount">Count of points</param>
		/// <returns>List of the brightness points</returns>
		public static List<Point> GetBrightestPoints(KrecImage grayImage, int pointsCount)
		{
			var brightestPoints = new List<Point>();
			int pixelsCount = grayImage.Width * grayImage.Height;

			var height = grayImage.Height;
			var width = grayImage.Width;
			var bytesPerLine = grayImage.BytesPerLine;
			var arrayOfMagnitudes = new double[pixelsCount];
			var targetIdx = 0;
			for (var rowIdx = 0; rowIdx < height; rowIdx++)
			{
				var rowStartIdx = rowIdx * bytesPerLine;
				for (var colIdx = 0; colIdx < width; colIdx++)
				{
					arrayOfMagnitudes[targetIdx] = grayImage.ImageData[rowStartIdx + colIdx] / 255.0;
					targetIdx++;
				}
			}
			
			arrayOfMagnitudes = FindMagnitudes(arrayOfMagnitudes, grayImage.Width);
			
			var indexesForNorm = new Tuple<int, double>[pixelsCount];
			for (var i = 0; i < pixelsCount; ++i)
			{
				indexesForNorm[i] = new Tuple<int, double>(i, arrayOfMagnitudes[i]);
			}
			
			var maxMagnitude = FindKthOrderStatistic(indexesForNorm, pointsCount, true);
			indexesForNorm = indexesForNorm.Skip(pixelsCount - pointsCount).ToArray();
			
			if (maxMagnitude < 0.00001)
				throw new Exception("Monotone image, can't determine scew angle");
			
			foreach (var index in indexesForNorm)
			{
				int x = index.Item1 % grayImage.Width;
				int y = index.Item1 / grayImage.Width;
				
				brightestPoints.Add(new Point(x, y));
			}
			
			if (brightestPoints.Count == 0)
				throw new Exception("Empty brightest points");
			
			return brightestPoints;
		}

		#region Wide bar graph
		/// <summary>
		/// Build bar graph by angles of brightest points
		/// </summary>
		/// <param name="barsStep">Step of bar graph</param>
		/// <param name="brigthestPoints">brightest points</param>
		/// <param name="width">Width of image. Used for centering.</param>
		/// <returns>Angle</returns>
		private static double FindAngleByWideBarGraph(double barsStep, List<Point> brigthestPoints, int width)
		{
			var bars = BuildWideBarGraph(barsStep, brigthestPoints, width);
			int barsCount = bars.Length;
			
			if (barsCount == 0)
				throw new Exception("Bad, bad bar graph");
			
			int max = -1, min = int.MaxValue;
			int maxIndex = 0;
			
			for (var i = 0; i < barsCount; ++i)
			{
				if (min > bars[i])
					min = bars[i];
				if (max < bars[i])
				{
					max = bars[i];
					maxIndex = i;
				}
			}
			
			if (min == bars[maxIndex]) //проверка нужна для корректной работы GetMaxAngleBordersInBargraph
				throw new Exception("Can't determine deskew angle, maybe monotone image");
			
			int distToCenter = barsCount / 2 - maxIndex; //Производится смещение, чтобы максимальное значение оказалось в центре. Нужно для соединения эквивалентных углов (0 и П)
			if (distToCenter > 0)
			{
				int takeCount = barsCount - distToCenter;
				bars = bars.Skip(takeCount).Concat(bars.Take(takeCount)).ToArray();
			}
			else if (distToCenter < 0)
			{
				bars = bars.Skip(-distToCenter).Concat(bars.Take(-distToCenter)).ToArray();
			}
			maxIndex += distToCenter;
			
			var borders = GetMaxAngleBordersInBargraph(bars, maxIndex);
			
			int sum = 0, sumWeight = 0;
			for (var i = borders.Item1; i <= borders.Item2; ++i)
			{
				sum += i * bars[i];
				sumWeight += bars[i];
			}
			if (sumWeight == 0)
				throw new Exception("Can't determine deskew angle, white image");
			
			var angle = (barsStep * sum) / sumWeight + Math.PI / 2 - distToCenter * barsStep;
			//PrintBarsAndBorders(@"resources\Images\RealHistory\2112_2.txt", -0.094, angle, bars, barsStep, distToCenter, borders.Item1, borders.Item2); //примерно так вызвается функция для тестирования
			
			return angle;
		}

		/// <summary>
		/// Get borders inside which angle will be calculated
		/// </summary>
		/// <param name="bars">Bar graph by angles</param>
		/// <param name="maxIndex">Index of maximal bar</param>
		/// <returns>Borders (Lower, Upper)</returns>
		private static Tuple<int, int> GetMaxAngleBordersInBargraph(int[] bars, int maxIndex)
		{
			const int maxMonotoneCount = 10; //Константы подобраны по истории
			
			int monotoneCount = 0;
			int lowerBorderIndex = maxIndex;
			for (var i = maxIndex; i > 0; --i)
			{
				if (bars[i] != bars[maxIndex])
				{
					++monotoneCount;
					if (monotoneCount == maxMonotoneCount)
					{
						lowerBorderIndex = i;
						break;
					}
				}
				else
				{
					lowerBorderIndex = i - 1;
					monotoneCount = 0;
				}
			}
			
			monotoneCount = 0;
			int upperBorderIndex = maxIndex;
			for (var i = maxIndex; i < bars.Count() - 1; ++i)
			{
				if (bars[i] != bars[maxIndex])
				{
					++monotoneCount;
					if (monotoneCount == maxMonotoneCount)
					{
						upperBorderIndex = i;
						break;
					}
				}
				else
				{
					upperBorderIndex = i + 1;
					monotoneCount = 0;
				}
			}
			
			return new Tuple<int, int>(lowerBorderIndex, upperBorderIndex);
		}

		/// <summary>
		/// Build a bar graph by angles for brightest points. 
		/// Each bar it's a line which goes througth the center of image
		/// </summary>
		/// <param name="angleStep">Step of bar graph</param>
		/// <param name="points">Points, whose angle will be used</param>
		/// <param name="width">Width of image. Used for centering.</param>
		/// <returns>Bar graph by angles</returns>
		private static int[] BuildWideBarGraph(double angleStep, List<Point> points, int width) //квадратное изображение
		{
			const double maxRadius = 3; //Толщина линии, в которую должны попадать точки. Подобрано эмпирически
			var angleCount = (int) (Math.PI / angleStep) + 1;
			var barGraph = new int[angleCount];
			
			var counter = 0;
			
			for (double angle = 0; angle < Math.PI; angle += angleStep, ++counter)
			{
				double cosA = Math.Cos(angle);
				double sinA = Math.Sin(angle);
				
				double cosA2 = Math.Cos(angle + Math.PI / 2);
				double sinA2 = Math.Sin(angle + Math.PI / 2);
				
				foreach (var point in points)
				{
					int nx = point.X - width / 2, ny = point.Y - width / 2;
					double r = Math.Abs(ny * cosA - nx * sinA);
					double r2 = Math.Abs(ny * cosA2 - nx * sinA2);
					if (r > maxRadius && r2 > maxRadius)
						continue;
					
					barGraph[counter]++;
				}
			}
			return barGraph;
		}

		/// <summary>
		/// Print bar graph and borders, inside which angle is calculating. Used for testing.
		/// </summary>
		/// <param name="outputPath"></param>
		/// <param name="realAngle"></param>
		/// <param name="determinedAngle"></param>
		/// <param name="bars"></param>
		/// <param name="maxError"></param>
		/// <param name="distToCenter"></param>
		/// <param name="lowerBorder"></param>
		/// <param name="upperBorder"></param>
		private static void PrintBarsAndBorders(string outputPath, double realAngle, double determinedAngle, int[] bars, double maxError, int distToCenter, int lowerBorder, int upperBorder)
		{
			while (Math.Abs(determinedAngle - Math.PI / 2 - realAngle) < Math.Abs(determinedAngle - realAngle))
				realAngle += Math.PI / 2;
			while (Math.Abs(determinedAngle + Math.PI / 2 - realAngle) < Math.Abs(determinedAngle - realAngle))
				realAngle -= Math.PI / 2;
			
			var sum = bars.Sum();
			
			File.WriteAllLines(outputPath, bars.Select((value, index) =>
				string.Format(
					"{1:F2}\t{2}\t{0:F5}" +
					(index == lowerBorder ? "<--" + string.Format(" {0:F3} - lower", realAngle) : " ") +
					(index == upperBorder ? "<--" + string.Format(" {0:F3} - upper", determinedAngle) : ""),
					value / (double) sum, index * maxError + Math.PI / 2 - distToCenter * maxError, value)));
			//string.Format("{1:F2}\t{0:F5}" + (index == lowerBorder ? "<--" + string.Format(" {0:F3} - real", realAngle) : index == upperBorder ? "<--" + string.Format(" {0:F3} - determined", determinedAngle) : ""), value, index * maxError + Math.PI / 2 - distToCenter * maxError, 2)));
			
		}
		#endregion

		#region Fft
		/// <summary>
		/// Find magnitudes of 2d Fourier transform results for image
		/// </summary>
		/// <param name="imageColors">Image colors from 0.0 to 1.0</param>
		/// <param name="width">Width of image</param>
		/// <returns>Magnitudes of Fourier transform results</returns>
		public static double[] FindMagnitudes(double[] imageColors, int width)
		{
			var matrix = new Complex[width][];
			for (var rowIdx = 0; rowIdx < width; rowIdx++)
			{
				matrix[rowIdx] = new Complex[width];
			}
			
			for (var rowIdx = 0; rowIdx < width; rowIdx++)
			{
				int row = rowIdx * width;
				var currentRow = matrix[rowIdx];
				for (var colIdx = 0; colIdx < width; ++colIdx)
				{
					currentRow[colIdx] = ((rowIdx + colIdx) % 2 != 0) ? -imageColors[row + colIdx] : imageColors[row + colIdx];
				}
			}
			
			Apply2DFft(matrix);
			
			var result = new double[width * width];
			var widthHalf = width / 2;
			
			var maxDistance = 2 * Square(widthHalf);
			for (var i = 0; i < width; ++i)
			{
				int row = i * width;
				for (var j = 0; j < width; ++j)
				{
					var distance = Square(i - widthHalf) + Square(j - widthHalf);
					// Такая весовая функция заставляет точки на частотной плоскости преобразования Фурье, отвечающие большим частотам
					// иметь больший вес при детектировании ориентации документа (опытным путем установлено, что так лучше).
					var weight = 0.5 + (distance / maxDistance) * 0.5;
					result[row + j] = Math.Min(matrix[i][j].Magnitude * 10 * weight, 1);
					// исходная версия весовой функции не учитывает частоту, которой соответствует точка на частотной плоскости преобразования Фурье
//					result[row + j] = Math.Min(matrix[i][j].Magnitude * 10, 1);
				}
			}
			
			return result;
		}

		/// <summary>
		/// Applies 2d Fourier transform to square matrix
		/// </summary>
		/// <param name="matrix">Square matrix</param>
		private static void Apply2DFft(Complex[][] matrix)
		{
			// TODO: кажется, что стоит попробовать обойтись без транспонирования, 
			// выполняя преобразование Фурье по строкам и по столбцам в отдельных реализациях
			// и на линейно упакованном массиве (т.е. на одномерном, а не двумерном)
			
			int width = matrix.Count();
			Parallel.For(0, width, i =>
			{
				Fft(matrix[i], width);
				for (var j = 0; j < width; ++j)
				{
					matrix[i][j] /= width;
				}
			});
			
			TransposeSquareMatrix(matrix);
			
			Parallel.For(0, width, i =>
			{
				Fft(matrix[i], width);
				for (var j = 0; j < width; ++j)
				{
					matrix[i][j] /= width;
				}
			});
			
			TransposeSquareMatrix(matrix);
		}

		/// <summary>
		/// Apply fast Fourier transform for array
		/// </summary>
		/// <param name="points">Array to transform. Size must be power of 2</param>
		/// /// <param name="width">Width of matrix. Used for more speed. (Maybe useless).</param>
		private static void Fft(Complex[] points, int width)
		{
			var outComplex = points;
			
			for (int i = 1, j = 0; i < width; ++i)
			{
				int bit = width >> 1;
				for (; j >= bit; bit >>= 1)
					j -= bit;
				j += bit;
				if (i < j)
				{
					Complex swap = outComplex[i];
					outComplex[i] = outComplex[j];
					outComplex[j] = swap;
				}
			}
			
			for (var len = 2; len <= width; len <<= 1) //Пересчёт углов всего заранее не даёт прироста в скорости
			{
				double ang = -2 * Math.PI / len;
				var wlen = new Complex(Math.Cos(ang), Math.Sin(ang));
				int len2 = len / 2;
				for (var i = 0; i < width; i += len)
				{
					Complex w = 1;
					for (int j = 0, ind1 = len2 + i; j < len2; ++j, ++ind1)
					{
						Complex u = outComplex[i + j];
						Complex v = outComplex[ind1] * w;
						outComplex[i + j] = (u + v);
						outComplex[ind1] = (u - v);
						w *= wlen;
					}
				}
			}
		}
		#endregion

		#region Additional functions
		/// <summary>
		/// Partition method to get index of temporary mediane
		/// </summary>
		/// <param name="array">Input array</param>
		/// <param name="leftBorder">Left border to search from</param>
		/// <param name="rightBorder">Right border to search to</param>
		/// <returns></returns>
		private static int GetPartition(Tuple<int, double>[] array, int leftBorder, int rightBorder)
		{
			var p = array[(rightBorder + leftBorder) / 2].Item2;
			int i = leftBorder, j = rightBorder;
			do
			{
				while (array[i].Item2 < p)
					i++;
				while (array[j].Item2 > p)
					j--;
				
				if (i < j)
				{
					var temp = array[i];
					array[i] = array[j];
					array[j] = temp;
					i++;
					j--;
				}
			} while (i < j);
			
			if (i == leftBorder)
				return i + 1;
			if (i == rightBorder)
				return i - 1;
			return i;
		}

		/// <summary>
		/// Kth order statistic implementation by using selection algorithm.
		/// https://en.wikipedia.org/wiki/Selection_algorithm
		/// </summary>
		/// <param name="array">Input array</param>
		/// <param name="order">Count of min/max elements to find</param>
		/// <param name="isMax">Toogle to search min/max</param>
		/// <returns></returns>
		private static double FindKthOrderStatistic(Tuple<int, double>[] array, int order, bool isMax)
		{
			int left = 0; 
			int right = array.Length - 1;
			if (isMax)
			{
				order = array.Length - order;
			}
			while (left < right - 1)
			{
				var mid = GetPartition(array, left, right);
				
				if (mid < order)
				{
					left = mid;
					continue;
				}
				if (order < mid)
				{
					right = mid;
					continue;
				}
				return array[order].Item2;
			}
			return array[order].Item2;
		}

		/// <summary>
		/// Transpose square matrix
		/// </summary>
		/// <typeparam name="T">Any type</typeparam>
		/// <param name="matrix">Square matrix</param>
		private static void TransposeSquareMatrix<T>(T[][] matrix)
		{
			int width = matrix.Count();
			for (int i = 0; i < width; ++i)
			{
				for (int j = i + 1; j < width; ++j)
				{
					T swap = matrix[j][i];
					matrix[j][i] = matrix[i][j];
					matrix[i][j] = swap;
				}
			}
		}

		/// <summary>
		/// Fix angle to make it acute
		/// </summary>
		/// <param name="angle">Input angle</param>
		/// <returns>Acute angle</returns>
		private static double CutAngle(double angle)
		{
			if (angle < -Math.PI / 2)
			{
				angle += Math.PI;
			}
			if (angle > Math.PI / 2)
			{
				angle -= Math.PI;
			}
			return angle;
		}

		private static double Square(double v)
		{
			return v * v;
		}
		#endregion
	}
}
