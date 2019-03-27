using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Kontur.Recognition.ImageCore;
using Kontur.Recognition.ImageDeskewer.ImageProcessing;

namespace Kontur.Recognition.ImageDeskewer.Utils
{
	/// <summary>
	/// Class for testing. Print some results of determiners work.
	/// </summary>
	static class ImagePrinter
	{
		/// <summary>
		/// Find magnitudes of Fourier transfotm and print it in file
		/// </summary>
		/// <param name="image">Square image wich sides size is power of 2</param>
		/// <param name="outputPath">Path to print image</param>
		public static void PrintFourierMagnitudes(KrecImage image, string outputPath)
		{
			int colorsCount = image.Width * image.Height;
			var resultBytes = new byte[image.Width * image.Height];
			
			var rc = new double[colorsCount];
			for (var i = 0; i < colorsCount; ++i)
				rc[i] = image.ImageData[i] / 255.0;
			
			rc = SkewAngleDeterminer.FindMagnitudes(rc, image.Width);
			
			for (var i = 0; i < colorsCount; ++i)
			{
				resultBytes[i] = (byte) Math.Min(255, rc[i] * 10000);
			}

			var krecImage = new KrecImage(
				image.Width, image.Height, image.Width, image.HorizontalResolution, image.VerticalResolution,
				image.Format, resultBytes);
			Print8BppImage(krecImage, outputPath);
		}

		/// <summary>
		/// Find magnitudes of Fourier transfotm and print it in file
		/// </summary>
		/// <param name="image">Square image wich sides size is power of 2</param>
		/// <param name="outputPath">Path to print image</param>
		public static void PrintFourierBrightestPoints(KrecImage image, string outputPath)
		{
			var resultBytes = new byte[image.Width * image.Height];
			var brightestPoints = SkewAngleDeterminer.GetBrightestPoints(image, 3000);
			
			foreach (var point in brightestPoints)
			{
				resultBytes[point.Y * image.Width + point.X] = 255;
			}

			var krecImage = new KrecImage(
				image.Width, image.Height, image.Width, image.HorizontalResolution, image.VerticalResolution,
				image.Format, resultBytes);
			Print8BppImage(krecImage, outputPath);
		}

		/// <summary>
		/// Print masked bitmap in file
		/// </summary>
		/// <param name="bmp">Printed bitmap</param>
		/// <param name="mask">Mask (true - will be printed)</param>
		/// <param name="outputPath">Path to print bitmap</param>
		/// <returns></returns>
		public static void PrintMaskedBitmap(Bitmap bmp, bool[,] mask, string outputPath)
		{
			var image = KrecImage.FromBitmap(bmp);
			var grayscaledImage = image.ToGrayscaled();
			PrintMaskedImage(grayscaledImage, mask, outputPath);
		}

		/// <summary>
		/// Print masked bitmap in file
		/// </summary>
		/// <param name="image">Printed image</param>
		/// <param name="mask">Mask (true - will be printed)</param>
		/// <param name="outputPath">Path to print image</param>
		/// <returns></returns>
		public static void PrintMaskedImage(KrecImage image, bool[,] mask, string outputPath)
		{
			var resultBytes = new byte[image.Width * image.Height];
			
			for (var y = 0; y < image.Height; ++y)
			{
				for (var x = 0; x < image.Width; ++x)
				{
					if (!mask[x, y])
						resultBytes[y * image.BytesPerLine + x] = 255;
				}
			}

			var krecImage = new KrecImage(
				image.Width, image.Height, image.Width, image.HorizontalResolution, image.VerticalResolution,
				image.Format, resultBytes);
			Print8BppImage(krecImage, outputPath);
		}

		/// <summary>
		/// Print bitmap with array of Boxes in file 
		/// </summary>
		/// <param name="boxes">Boxes which will be printed.</param>
		/// <param name="grayscaleImage">Image which will be printed.</param>
		/// <param name="outputPath">Path to print image</param>
		/// <param name="minPrintHeight">Minimal height of Boxes which will be printed. Null if doesn't metter.</param>
		/// <param name="maxPrintHeight">Maximal height of Boxes which will be printed. Null if doesn't metter.</param>
		public static void PrintBoxes(IEnumerable<Rectangle> boxes, KrecImage grayscaleImage, string outputPath, int? minPrintHeight = null, int? maxPrintHeight = null)
		{
			var newWidth = grayscaleImage.Width;
			var newHeight = grayscaleImage.Height;
			var oldBytesPerLine = grayscaleImage.BytesPerLine;
			var newBytesPerLine = KrecImage.CalculateStride(newWidth, KrecImagePixelFormat.Format24bppRgb);

			var newImageData = new byte[newBytesPerLine * newHeight * 3];

			for (int rowIdx = 0; rowIdx < newHeight; rowIdx++)
			{
				var oldRowStartIdx = rowIdx * oldBytesPerLine;
				var newRowStartIdx = rowIdx * newBytesPerLine;

				for (int colIdx = 0; colIdx < newWidth; colIdx++)
				{
					int ix = newRowStartIdx + colIdx * 3;
					newImageData[ix + 2] = newImageData[ix + 1] = newImageData[ix] = grayscaleImage.ImageData[oldRowStartIdx + colIdx];
				}
			}

			int boxCounter = 0;
			foreach (var box in boxes)
			{
				if ((maxPrintHeight != null && box.Height > maxPrintHeight) || (minPrintHeight != null && box.Height < minPrintHeight))
					continue;
				for (var i = box.Y; i < box.Y + box.Height && i < grayscaleImage.Height; ++i)
				{
					for (var j = box.X; j < box.X + box.Width && j < grayscaleImage.Width; ++j)
					{
						int pixelIdx = (i * newBytesPerLine + j) * 3;
						newImageData[pixelIdx] = 255;
						newImageData[pixelIdx + 1] = (byte) ((boxCounter % 2) * 255);
						newImageData[pixelIdx + 2] = 0;
					}
				}
				++boxCounter;
			}

			var krecImage = new KrecImage(
				newWidth, newHeight, newBytesPerLine, 
				grayscaleImage.HorizontalResolution, grayscaleImage.VerticalResolution,
				KrecImagePixelFormat.Format24bppRgb, newImageData);
			Print24BppImage(krecImage, outputPath);
		}

		/// <summary>
		/// Print bitmap with array of Boxes in file 
		/// </summary>
		/// <param name="boxes">Boxes which will be printed.</param>
		/// <param name="bmp">Bitmap which will be printed.</param>
		/// <param name="outputPath">Path to print bitmap</param>
		/// <param name="minPrintHeight">Minimal height of Boxes which will be printed. Null if doesn't metter.</param>
		/// <param name="maxPrintHeight">Maximal height of Boxes which will be printed. Null if doesn't metter.</param>
		public static void PrintBoxes(IEnumerable<Rectangle> boxes, Bitmap bmp, string outputPath, int? minPrintHeight = null, int? maxPrintHeight = null)
		{
			var image = KrecImage.FromBitmap(bmp);
			var grayscaledImage = image.ToGrayscaled();
			PrintBoxes(boxes, grayscaledImage, outputPath, maxPrintHeight, maxPrintHeight);
		}

		/// <summary>
		/// Print image with 8bppIndexed pixel format (before printing convert it to 24bpp)
		/// </summary>
		/// <param name="image">Image with 8bppIndexed pixel format</param>
		/// <param name="outputPath">Path to print image</param>
		public static void Print8BppImage(KrecImage image, string outputPath)
		{
			var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
			bmp.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			int len = image.Width * image.Height * 3;
			var result = new byte[len];
			for (var i = 0; i < len; i += 3)
			{
				int ind = i / 3;
				result[i] = result[i + 1] = result[i + 2] = image.ImageData[ind];
			}
			
			bmp.WithBitmapData(bitmapData =>
			{
				var grayscaleValues = BitmapProcessor.Reconstruct24To24BppRgbBitmap(bitmapData, result);
				IntPtr ptr = bitmapData.Scan0;
				Marshal.Copy(grayscaleValues, 0, ptr, grayscaleValues.Length);
			});
			bmp.Save(outputPath);
		}

		/// <summary>
		/// Print image with 24bppRgb pixel format
		/// </summary>
		/// <param name="image">Image with 24bppRgb pixel format</param>
		/// <param name="outputPath">Path to print image</param>
		public static void Print24BppImage(KrecImage image, string outputPath)
		{
			var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
			bmp.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			bmp.WithBitmapData(bitmapData =>
			{
				var rgbValues = BitmapProcessor.Reconstruct24To24BppRgbBitmap(bitmapData, image.ImageData);
				IntPtr ptr = bitmapData.Scan0;
				Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);
			});
			bmp.Save(outputPath);
		}


		/// <summary>
		/// Get binary, blured by orientation determiner. Used for PrintMaskedBinary. Copy of OrientationDeterminer.GetBluredBoundingRectsSums.
		/// </summary>
		/// <param name="boundingRects"></param>
		/// <param name="inpBinary"></param>
		/// <returns></returns>
		public static bool[,] GetBluredByRectanglesBinary(IEnumerable<Rectangle> boundingRects, bool[,] inpBinary)
		{
			var binary = (bool[,]) inpBinary.Clone();
			
			foreach (var rect in boundingRects)
			{
				binary = GetBluredByBoundingRectBinary(rect, binary);
			}
			return binary;
		}

		/// <summary>
		/// Copy of OrientationDeterminer.GetBluredByBoundingRectBinarySums
		/// </summary>
		/// <param name="boundingRect"></param>
		/// <param name="binary"></param>
		/// <returns></returns>
		private static bool[,] GetBluredByBoundingRectBinary(Rectangle boundingRect, bool[,] binary)
		{
			int dist = 2 * boundingRect.Height;
			var sums = new List<int>();
			for (int y = boundingRect.Top; y < boundingRect.Bottom; y++)
			{
				int sum = 0;
				int lastBlackX = boundingRect.Left - 1;
				for (int x = boundingRect.Left; x < boundingRect.Right; ++x)
				{
					if (binary[x, y])
					{
						if (x - lastBlackX < dist)
						{
							sum += x - lastBlackX;
							for (var nx = lastBlackX + 1; nx < x; ++nx)
								binary[nx, y] = true;
						}
						else
							++sum;
						
						lastBlackX = x;
					}
				}
				if (boundingRect.Right - lastBlackX < dist)
				{
					for (var nx = lastBlackX + 1; nx < boundingRect.Right; ++nx)
						binary[nx, y] = true;
					sum += boundingRect.Right - lastBlackX - 1;
				}
				sums.Add(sum);
			}
			return binary;
		}
	}
}
