using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Kontur.Recognition.GeometryModel;
using Kontur.Recognition.ImageBinarizer;
using Kontur.Recognition.ImageCore;

namespace Kontur.Recognition.ImageDeskewer.ImageProcessing
{
	/// <summary>
	/// Contains different operations with bitmaps
	/// </summary>
	public static class BitmapProcessor
	{
		/// <summary>
		/// Performs given action on extracted bitmap data. After action is performed, bitmap is unlocked in memory 
		/// (so bitmap data should not be referenced outside of the action)
		/// </summary>
		/// <param name="bmp">The bitmap to process</param>
		/// <param name="bitmapDataAction">Action to perform</param>
		public static T WithBitmapData<T>(this Bitmap bmp, Func<BitmapData, T> bitmapDataAction)
		{
			var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			var bitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
			try
			{
				return bitmapDataAction(bitmapData);
			}
			finally
			{
				bmp.UnlockBits(bitmapData);
			}
		}

		/// <summary>
		/// Performs given action on extracted bitmap data. After action is performed, bitmap is unlocked in memory 
		/// (so bitmap data should not be referenced outside of the action)
		/// </summary>
		/// <param name="bmp">The bitmap to process</param>
		/// <param name="bitmapDataAction">Action to perform</param>
		public static void WithBitmapData(this Bitmap bmp, Action<BitmapData> bitmapDataAction)
		{
			WithBitmapData(bmp, bitmapData =>
			{
				bitmapDataAction(bitmapData);
				return 0;
			});
		}

		/// <summary>
		/// Set colors in bitmap
		/// </summary>
		/// <param name="bmp">Bitmap to set</param>
		/// <param name="colors">Seted colors</param>
		public static void SetColorValues(this Bitmap bmp, byte[] colors)
		{
			bmp.WithBitmapData(bmpData => Marshal.Copy(colors, 0, bmpData.Scan0, colors.Length));
		}

		/// <summary>
		/// Get colors in bitmaps format
		/// </summary>
		/// <param name="bmpData">Bitmap Data</param>
		/// <returns>Colors in bitmaps format</returns>
		public static byte[] GetColorValues(this BitmapData bmpData)
		{
			var size = bmpData.Stride * bmpData.Height;
			var pointer = bmpData.Scan0;
			
			// Declare an array to hold the bytes of the bitmap. 
			var colorValues = new byte[size];
			
			// TODO: avoid use of Marshal.Copy (use unsafe byte* instead to extract image bytes)
			// Copy the RGB values into the array.
			Marshal.Copy(pointer, colorValues, 0, size);
			return colorValues;
		}

		/// <summary>
		///Used for tests
		/// </summary>
		/// <param name="bmpData">24 bit rgb bitmap data</param>
		/// <param name="colors">24bpp colors</param>
		/// <returns>Reconstructed colors (taking into account stride)</returns>
		public static byte[] Reconstruct24To24BppRgbBitmap(BitmapData bmpData, byte[] colors)
		{
			if (bmpData.PixelFormat != PixelFormat.Format24bppRgb)
				throw new BadImageFormatException("Bad pixel format");
			
			var size = Math.Abs(bmpData.Stride) * bmpData.Height;
			var rgbValues = new byte[size];
			
			var stride = bmpData.Stride;
			var width = bmpData.Width;
			
			for (var i = 0; i < colors.Length / 3; i++)
			{
				int ix = stride * (i / width) + (i % width) * 3;
				int re = i * 3;
				rgbValues[ix] = colors[re];
				rgbValues[ix + 1] = colors[re + 1];
				rgbValues[ix + 2] = colors[re + 2];
			}
			
			return rgbValues;
		}

		/// <summary>
		/// Loads image from file, binarize and rotate it and returns KrecImage
		/// </summary>
		/// <param name="path">Image path</param>
		/// <returns>Binarized and rotated image with deskew parameters</returns>
		public static KrecImage LoadAlignedBinarisedImageFromFile(string path)
		{
			DeskewParameters unused;
			return LoadAlignedBinarisedImageFromFile(path, out unused);
		}

		/// <summary>
		/// Loads image from file, binarize and rotate it and returns KrecImage
		/// </summary>
		/// <param name="path">Image path</param>
		/// <param name="deskewParameters">The information on how the image has been deskewed</param>
		/// <returns>Binarized and rotated image with deskew parameters</returns>
		public static KrecImage LoadAlignedBinarisedImageFromFile(string path, out DeskewParameters deskewParameters)
		{
			var sourceImage = KrecImage.FromFile(path);
			var binarizedImage = sourceImage.Binarize();

			deskewParameters = FastImageDeskewer.GetDeskewParameters(binarizedImage);
			return RotateGrayscaleImage(binarizedImage, deskewParameters.AngleRadians);
		}

		/// <summary>
		/// Rotate KrecImage
		/// </summary>
		/// <param name="grayscaleImage">Rotated image</param>
		/// <param name="rotationAngle">Rotation angle</param>
		/// <returns>Rotated image</returns>
		public static KrecImage RotateGrayscaleImage(this KrecImage grayscaleImage, double rotationAngle)
		{
			if (grayscaleImage.Format.BytesPerPixel() > 1)
			{
				throw new ArgumentException("Can not rotate image with more than 1 byte per pixel");
			}

			rotationAngle *= -1; //Из-за обратной системы координат

			var sourceWidth = grayscaleImage.Width;
			var sourceHeight = grayscaleImage.Height;
			var sourceBytesPerLine = grayscaleImage.BytesPerLine;

			double diag = Math.Sqrt(sourceWidth * sourceWidth + sourceHeight * sourceHeight);
			// TODO: разобраться. Мутная логика пересчета углов	для получения размера целевого изображения
			// TODO: должно быть достаточно повернуть диагональ без дополнительной тригонометрии
			double diagAngle = Math.Atan(sourceHeight / (double)sourceWidth);
			double cosDiag = Math.Max(Math.Abs(Math.Cos(rotationAngle + diagAngle)),
				Math.Abs(Math.Cos(rotationAngle - diagAngle)));
			double sinDiag = Math.Max(Math.Abs(Math.Sin(rotationAngle + diagAngle)),
				Math.Abs(Math.Sin(rotationAngle - diagAngle)));
			
			int newWidth = (int) (diag * cosDiag + 0.5);
			int newHeight = (int) (diag * sinDiag + 0.5);
			int newBytesPerLine = KrecImage.CalculateStride(newWidth, grayscaleImage.Format);
			
			double sinA = Math.Sin(rotationAngle);
			double cosA = Math.Cos(rotationAngle);
			
			
			double x0 = (sourceWidth - cosA * newWidth + sinA * newHeight) / 2;
			double y0 = (sourceHeight - sinA * newWidth - cosA * newHeight) / 2;

			var newImageData = new byte[newBytesPerLine * newHeight];
			var sourceImageData = grayscaleImage.ImageData;

			for (var newY = 0; newY < newHeight; newY++)
			{
				var deltaX = -sinA * newY + x0;
				var deltaY = cosA * newY + y0;
				var newImageLineIdx = newY * newBytesPerLine;
				for (var newX = 0; newX < newWidth; newX++)
				{
					int x = (int)(cosA * newX + deltaX); //Рассчёт новых координат заранее не даёт прироста в скорости
					int y = (int)(sinA * newX + deltaY);
					if (x >= sourceWidth || y >= sourceHeight || x < 0 || y < 0)
						newImageData[newImageLineIdx + newX] = 255;
					else
						newImageData[newImageLineIdx + newX] = sourceImageData[y * sourceBytesPerLine + x];
				}
			}

			return new KrecImage(
				newWidth, newHeight, newBytesPerLine, 
				grayscaleImage.HorizontalResolution, grayscaleImage.VerticalResolution,
				grayscaleImage.Format, newImageData);
		}

		/// <summary>
		/// Convert image to binary format
		/// </summary>
		/// <param name="grayscaleImage">Grayscale image</param>
		/// <returns>0 - white, 1 - black</returns>
		public static bool[,] BinarizeGrayscaleImage(KrecImage grayscaleImage)
		{
			var threshold = OtsuThreshold(grayscaleImage);
			
			var binary = new bool[grayscaleImage.Width, grayscaleImage.Height];
			var height = grayscaleImage.Height;
			var width = grayscaleImage.Width;
			var bytesPerLine = grayscaleImage.BytesPerLine;
			var imageData = grayscaleImage.ImageData;
			for (var rowIdx = 0; rowIdx < height; rowIdx++)
			{
				var rowStartIdx = rowIdx * bytesPerLine;
				for (int colIdx = 0; colIdx < width; colIdx++)
				{
					binary[colIdx, rowIdx] = imageData[rowStartIdx + colIdx] <= threshold;
				}
			}
			return binary;
		}

		/// <summary>
		/// Get bar grapgh for colors in greyscale
		/// </summary>
		/// <param name="grayscaledImage">Greyscaled image to process</param>
		/// <returns>Bar graph</returns>
		public static int[] GetColorsBarGraph(KrecImage grayscaledImage)
		{
			var barGraph = new int[256]; //в массиве будет не более 256 элементов

			var width = grayscaledImage.Width;
			var height = grayscaledImage.Height;
			var bytesPerLine = grayscaledImage.BytesPerLine;
			var imageData = grayscaledImage.ImageData;
			for (var rowIdx = 0; rowIdx < height; rowIdx++)
			{
				var rowStartIdx = rowIdx * bytesPerLine;
				for (var colIdx = 0; colIdx < width; colIdx++)
				{
					barGraph[imageData[rowStartIdx + colIdx]]++;
				}
			}

			return barGraph;
		}

		/// <summary>
		/// Find threshold by Otsu's method
		/// </summary>
		/// <param name="grayscaledImage">Greyscaled image to process</param>
		/// <returns>Threshold</returns>
		public static int OtsuThreshold(KrecImage grayscaledImage)
		{
			return OtsuThreshold(GetColorsBarGraph(grayscaledImage));
		}

		/// <summary>
		/// Find threshold by Otsu's method
		/// </summary>
		/// <param name="barGraph">Bar graph of colors</param>
		/// <param name="lowerBorder"></param>
		/// <param name="upperBorder"></param>
		/// <returns>Threshold</returns>
		private static int OtsuThreshold(int[] barGraph, int lowerBorder = 0, int upperBorder = 255)
		{
			const double epsilon = 1e-15;
			int min = 255, max = 0;
			
			for (var color = lowerBorder; color < upperBorder + 1; ++color)
			{
				if (barGraph[color] != 0 && color < min)
					min = color;
				
				if (barGraph[color] != 0 && color > max)
					max = color;
			}
			
			double mu = 0, scale = 1.0 / barGraph.Sum();
			
			for (var i = min; i <= max; i++)
				mu += i * (double) barGraph[i];
			
			mu *= scale;
			double mu1 = 0, q1 = 0;
			double maxSigma = -1;
			var threshold = 0;
			
			for (var i = min; i <= max; i++)
			{
				double pI = barGraph[i] * scale;
				mu1 *= q1;
				q1 += pI;
				double q2 = 1.0 - q1;
				
				if (Math.Min(q1, q2) < epsilon || Math.Max(q1, q2) > 1.0 - epsilon)
					continue;
				
				mu1 = (mu1 + i * pI) / q1;
				double mu2 = (mu - q1 * mu1) / q2;
				double sigma = q1 * q2 * (mu1 - mu2) * (mu1 - mu2);
				if (sigma > maxSigma)
				{
					maxSigma = sigma;
					threshold = i;
				}
			}
			return threshold;
		}

		/// <summary>
		/// Align background of image to white color
		/// </summary>
		/// <param name="image">Image to align background</param>
		/// <returns>Image with aligned bacground</returns>
		public static KrecImage AlignImageBackround(KrecImage image)
		{
			var barGraph = GetColorsBarGraph(image);
			int threshold = OtsuThreshold(barGraph);
			int threshold2 = OtsuThreshold(barGraph, 0, threshold);

			var height = image.Height;
			var width = image.Width;
			var bytesPerLine = image.BytesPerLine;
			var imageData = image.ImageData;

			for (var rowIdx = 0; rowIdx < height; rowIdx++)
			{
				var rowStartIdx = rowIdx * bytesPerLine;
				for (var colIdx = 0; colIdx < width; colIdx++)
				{
					var pixelIdx = rowStartIdx + colIdx;
					if (imageData[pixelIdx] > threshold2)
					{
						imageData[pixelIdx] = 255;
					}
				}
			}
			return image;
		}
	}
}
