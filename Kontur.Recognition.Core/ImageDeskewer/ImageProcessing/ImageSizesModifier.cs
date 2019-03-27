using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontur.Recognition.ImageCore;
using Kontur.Recognition.Utils;

namespace Kontur.Recognition.ImageDeskewer.ImageProcessing
{
	/// <summary>
	/// Modify image sizes by different condition
	/// </summary>
	public static class ImageSizesModifier
	{
		/// <summary>
		/// Inscribes the given grayscaled image into rectangle of the specified size.
		/// Proportions are preserved.  If the target dimensions are smaller than 
		/// original one, the image gets resized.
		/// </summary>
		/// <param name="grayscaleImage">Image to inscribe</param>
		/// <param name="newWidth">New width</param>
		/// <param name="newHeight">New height</param>
		/// <returns>Completed image</returns>
		public static KrecImage GetCompletedToSizeGrayscaleImage(KrecImage grayscaleImage, 
			int newWidth, int newHeight)
		{
			if (grayscaleImage.Format.BytesPerPixel() > 1)
				throw new ArgumentException("Only grayscaled images are supported");

			double kx = 1;
			double ky = 1;
			
			if (newWidth < grayscaleImage.Width)
				kx = (double)grayscaleImage.Width / newWidth;
			
			if (newHeight < grayscaleImage.Height)
				ky = (double)grayscaleImage.Height / newHeight;
			
			double k = Math.Max(kx, ky);

			int newBytesPerLine = KrecImage.CalculateStride(newWidth, grayscaleImage.Format);
			int len = newBytesPerLine * newHeight;
			var newColors = new byte[len];

			var emptyLine = new byte[newBytesPerLine];
			emptyLine.FillWith((byte)255);

			// The width and the height of source image in target image coordinates
			int sourceWidthScaled = (int)(grayscaleImage.Width / k);
			int sourceHeightScaled = (int)(grayscaleImage.Height / k);
			
			// The coordinates of source image in target image coordinates
			int startX = Math.Max((newWidth - sourceWidthScaled) / 2, 0);
			int endX = startX + sourceWidthScaled;
			int startY = (newHeight - sourceHeightScaled) / 2;
			int endY = startY + sourceHeightScaled;

			for (var newRowIdx = 0; newRowIdx < startY; newRowIdx++)
			{
				Array.Copy(emptyLine, 0, newColors, newRowIdx * newBytesPerLine, newBytesPerLine);
			}

			for (var newRowIdx = endY; newRowIdx < newHeight; newRowIdx++)
			{
				Array.Copy(emptyLine, 0, newColors, newRowIdx * newBytesPerLine, newBytesPerLine);
			}

			byte[] sourceData = grayscaleImage.ImageData;
			double oldRowIdxFloat = 0;
			for (var newRowIdx = startY; newRowIdx < endY; oldRowIdxFloat += k, newRowIdx++)
			{
				int oldRowIdx = (int) (oldRowIdxFloat);
				if (oldRowIdx >= grayscaleImage.Height)
				{
					Array.Copy(emptyLine, 0, newColors, newRowIdx * newBytesPerLine, newBytesPerLine);
					continue;
				}

				int newRowStartIdx = newRowIdx * newBytesPerLine;
				int oldRowStartIdx = oldRowIdx * grayscaleImage.BytesPerLine;
				double oldX = 0;

				// TODO: check that we do not get vertical black line at the right side of scaled image
				for (var newX = startX; oldX < grayscaleImage.Width; ++newX, oldX += k)
				{
					newColors[newRowStartIdx + newX] = sourceData[oldRowStartIdx + (int) oldX];
				}

				if (startX > 0)
					Array.Copy(emptyLine, 0, newColors, newRowStartIdx, startX);
				if (endX < newWidth)
					Array.Copy(emptyLine, 0, newColors, newRowStartIdx + endX, newWidth - endX);
			}

			return new KrecImage(newWidth, newHeight, newBytesPerLine,
				(float) (grayscaleImage.HorizontalResolution * k), (float) (grayscaleImage.VerticalResolution * k),
				grayscaleImage.Format, newColors);
		}

		/// <summary>
		/// Compress image to specified sizes
		/// </summary>
		/// <param name="grayscaleImage">Original image in grayscale</param>
		/// <param name="newWidth">Result width</param>
		/// <param name="newHeight">Result height</param>
		/// <returns>Compressed image</returns>
		public static KrecImage CompressImageToNewSizes(KrecImage grayscaleImage, int newWidth, int newHeight)
		{
			var kx = grayscaleImage.Width / (double) newWidth;
			var ky = grayscaleImage.Height / (double) newHeight;
			var oldBytesPerLine = grayscaleImage.BytesPerLine;
			var newBytesPerLine = KrecImage.CalculateStride(newWidth, grayscaleImage.Format);

			var newImageData = new byte[newHeight * newBytesPerLine];
			
			for (var newRowIdx = 0; newRowIdx < newHeight; newRowIdx++)
			{
				var oldRowIdx = (int) Math.Round(newRowIdx * ky);
				if (oldRowIdx == grayscaleImage.Height)
				{
					oldRowIdx--;
				}

				var oldRowStartIdx = oldRowIdx * oldBytesPerLine;
				var newRowStartIdx = newRowIdx * newBytesPerLine;
				for (var newColIdx = 0; newColIdx < newWidth; newColIdx++)
				{
					var oldColIdx = (int) Math.Round(kx * newColIdx);
					if (oldColIdx == grayscaleImage.Width)
					{
						--oldColIdx;
					}
					newImageData[newRowStartIdx + newColIdx] = grayscaleImage.ImageData[oldRowStartIdx + oldColIdx];
				}
			}
			return new KrecImage(newWidth, newHeight, newBytesPerLine, (float) (grayscaleImage.HorizontalResolution / kx), (float) (grayscaleImage.VerticalResolution / ky), grayscaleImage.Format, newImageData);
		}

		/// <summary>
		/// Find points coordinates in image, before it was rotated
		/// </summary>
		/// <param name="rotatedPoints">Points which coordinates are founds</param>
		/// <param name="rotationAngle">Rotation angle</param>
		/// <param name="originalImageWidth">Original image width</param>
		/// <param name="originalImageHeight">Original image height</param>
		/// <returns>Original coordinates of points</returns>
		public static IEnumerable<Point> GetPointsBeforeRotation(IEnumerable<Point> rotatedPoints, double rotationAngle, int originalImageWidth, int originalImageHeight)
		{
			rotationAngle *= -1; //Из-за обратной системы координат
			
			double diag = Math.Sqrt(originalImageWidth * originalImageWidth + originalImageHeight * originalImageHeight);
			double diagAngle = Math.Atan(originalImageHeight / (double) originalImageWidth);
			double cosDiag = Math.Max(Math.Abs(Math.Cos(rotationAngle + diagAngle)),
				Math.Abs(Math.Cos(rotationAngle - diagAngle)));
			double sinDiag = Math.Max(Math.Abs(Math.Sin(rotationAngle + diagAngle)),
				Math.Abs(Math.Sin(rotationAngle - diagAngle)));
			
			var newWidth = (int) (diag * cosDiag + 0.5);
			var newHeight = (int) (diag * sinDiag + 0.5);
			
			double sinA = Math.Sin(rotationAngle);
			double cosA = Math.Cos(rotationAngle);
			
			double x0 = (originalImageWidth - cosA * newWidth + sinA * newHeight) / 2;
			double y0 = (originalImageHeight - sinA * newWidth - cosA * newHeight) / 2;
			
			return rotatedPoints.Select(point =>
			{
				var x = (int) (cosA * point.X - sinA * point.Y + x0);
				var y = (int) (sinA * point.X + cosA * point.Y + y0);
				return new Point(x, y);
			});
		}

		/// <summary>
		/// Rotate only sime points of image
		/// </summary>
		/// <param name="rotatedPoints">Rotated points</param>
		/// <param name="rotationAngle">Rotation angle</param>
		/// <param name="originalImageWidth">Image width</param>
		/// <param name="originalImageHeight">Image height</param>
		/// <returns>Rotated points</returns>
		public static IEnumerable<Point> RotatePoints(IEnumerable<Point> rotatedPoints, double rotationAngle, int originalImageWidth, int originalImageHeight)
		{
			rotationAngle *= -1; //Из-за обратной системы координат
			
			double diag = Math.Sqrt(originalImageWidth * originalImageWidth + originalImageHeight * originalImageHeight);
			double diagAngle = Math.Atan(originalImageHeight / (double) originalImageWidth);
			double cosDiag = Math.Max(Math.Abs(Math.Cos(rotationAngle + diagAngle)),
				Math.Abs(Math.Cos(rotationAngle - diagAngle)));
			double sinDiag = Math.Max(Math.Abs(Math.Sin(rotationAngle + diagAngle)),
				Math.Abs(Math.Sin(rotationAngle - diagAngle)));
			
			var newWidth = (int) (diag * cosDiag + 0.5);
			var newHeight = (int) (diag * sinDiag + 0.5);
			
			double sinA = Math.Sin(rotationAngle);
			double cosA = Math.Cos(rotationAngle);
			
			double x0 = (originalImageWidth - cosA * newWidth + sinA * newHeight) / 2;
			double y0 = (originalImageHeight - sinA * newWidth - cosA * newHeight) / 2;
			
			var result = new List<Point>();
			
			foreach (var point in rotatedPoints)
			{
				int dx = (int) (point.X - x0 + 0.5);
				int dy = (int) (point.Y - y0 + 0.5);
				
				int x = (int) (cosA * dx + sinA * dy + 0.5);
				int y = (int) (-sinA * dx + cosA * dy + 0.5);
				result.Add(new Point(x, y));
			}
			
			return result;
		}

		/// <summary>
		/// Complete images to power of 2 sizes by white lines. If images width or height more then maxSideWith then compress image.
		/// </summary>
		/// <param name="originalImage">Image to complete</param>
		/// <param name="maxSideWidth">Max completion side width</param>
		/// <returns>Completed image</returns>
		public static KrecImage GetImageWithPowerOf2Side(KrecImage originalImage, int maxSideWidth)
		{
			var newWidth = Math.Max(originalImage.Width, originalImage.Height);
			if ((newWidth & (newWidth - 1)) != 0) // the fasterst way to check that the number is a power of two
			{
				var exp = (int) Math.Log(newWidth, 2);
				newWidth = (int) Math.Pow(2, exp + 1);
			}
			
			newWidth = Math.Min(newWidth, maxSideWidth);
			
			if (newWidth == originalImage.Width && newWidth == originalImage.Height)
				return originalImage;
			
			return GetCompletedToSizeGrayscaleImage(originalImage, newWidth, newWidth);
		}
	}
}
