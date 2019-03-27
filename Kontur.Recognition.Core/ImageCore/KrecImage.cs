using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Kontur.Recognition.ImageDeskewer.ImageProcessing;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.ImageCore
{
	/// <summary>
	/// Platform independent representation of bitmap image in a managed memory.
	/// Note: semantics of this class is the same as of Bitmap with the only difference that the data
	/// are stored in managed memory, so it allows to perform chains of conversions in managed code 
	/// without copying data between unmanaged and managed memory.
	/// </summary>
	public class KrecImage
	{
		private readonly int width;
		private readonly int height;
		private readonly int bytesPerLine;
		private readonly float horizontalResolution;
		private readonly float verticalResolution;
		private readonly KrecImagePixelFormat format;
		private readonly byte[] imageData;

		public int Width { get { return width; }}
		public int Height { get { return height; }}

		public int BytesPerLine { get { return bytesPerLine; }}

		public float HorizontalResolution { get { return horizontalResolution; }}
		public float VerticalResolution { get { return verticalResolution; }}

		public KrecImagePixelFormat Format { get { return format; }}

		public byte[] ImageData { get { return imageData; }}

		public KrecImage(int width,
			int height,
			int bytesPerLine,
			float horizontalResolution,
			float verticalResolution,
			KrecImagePixelFormat pixelFormat,
			[NotNull] byte[] imageData)
		{
			this.width = width;
			this.height = height;
			this.bytesPerLine = bytesPerLine;
			this.horizontalResolution = horizontalResolution;
			this.verticalResolution = verticalResolution;
			format = pixelFormat;
			this.imageData = imageData;

			var bytesPerPixel = pixelFormat.BytesPerPixel();
			if (bytesPerLine < bytesPerPixel * width)
				throw new ArgumentException(
					string.Format("Number of bytes per line ({0}) is less than needed for the specified image width ({1}) and pixel format ({2})",
						bytesPerLine, width, pixelFormat));
			if (imageData.Length < (height * bytesPerLine))
			{
				throw new ArgumentException(
					string.Format("Pixel data array for image is too small (size = {0}) for given bytes per line ({1}) and height ({2})",
						imageData.Length, bytesPerLine, height));
			}
		}

		public KrecImage(KrecImage source, byte[] newImageData, KrecImagePixelFormat newKrecImagePixelFormat)
			: this(source.Width, source.Height, source.BytesPerLine, source.HorizontalResolution,
				source.VerticalResolution, newKrecImagePixelFormat, newImageData)
		{
		}

		public KrecImage(KrecImage source, byte[] newImageData)
			: this(source, newImageData, source.Format)
		{
		}

		/// <summary>
		/// Creates a new image object from the specified file
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static KrecImage FromFile(string path)
		{
			using (var image = Image.FromFile(path))
			{
				using (var sourceBitmap = image as Bitmap ?? new Bitmap(image))
				{
					return FromBitmap(sourceBitmap);
				}
			}
		}

		/// <summary>
		/// Creates a new image object from the given bitmap object
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static KrecImage FromBitmap(Bitmap bitmap)
		{
			KrecImagePixelFormat pixelFormat;
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed:
					pixelFormat = KrecImagePixelFormat.Format1BitPerPixel;
					break;
				case PixelFormat.Format8bppIndexed:
					pixelFormat = KrecImagePixelFormat.Format8BitPerPixel;
					break;
				case PixelFormat.Format24bppRgb:
					pixelFormat = KrecImagePixelFormat.Format24bppRgb;
					break;
				case PixelFormat.Format32bppArgb:
					pixelFormat = KrecImagePixelFormat.Format32bppArgb;
					break;
				case PixelFormat.Format32bppPArgb:
					pixelFormat = KrecImagePixelFormat.Format32bppPArgb;
					break;
//				case PixelFormat.Format4bppIndexed:
//				case PixelFormat.Format16bppGrayScale:
//				case PixelFormat.Format16bppRgb555:
//				case PixelFormat.Format16bppRgb565:
//				case PixelFormat.Format16bppArgb1555:
//				case PixelFormat.Format32bppRgb:
//				case PixelFormat.Format48bppRgb:
//				case PixelFormat.Format64bppArgb:
//				case PixelFormat.Format64bppPArgb:
				default:
					throw new ArgumentOutOfRangeException();
			}

			return bitmap.WithBitmapData(data =>
				                             new KrecImage(data.Width, data.Height, data.Stride,
					                             bitmap.HorizontalResolution, bitmap.VerticalResolution,
					                             pixelFormat, data.GetColorValues()));
		}

		public Bitmap ToBitmap()
		{
			var pixelFormat = format.ToPixelFormat();
			var bitmap = new Bitmap(width, height, pixelFormat);
			bitmap.SetResolution(horizontalResolution, verticalResolution);

			if (pixelFormat == PixelFormat.Format8bppIndexed)
			{
				var palette = bitmap.Palette;
				var colorEntries = palette.Entries;
				for (var colorIndex = 0; colorIndex < 256; colorIndex++)
				{
					colorEntries[colorIndex] = Color.FromArgb(colorIndex, colorIndex, colorIndex);
				}

				bitmap.Palette = palette;
			}

			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, pixelFormat);
			try
			{
				if (bitmapData.Stride != bytesPerLine)
				{
					var bytes = new byte[bitmapData.Height * bitmapData.Stride];
					for (var scanLineIdx = 0; scanLineIdx < height; scanLineIdx++)
					{
						Array.Copy(imageData, scanLineIdx * bytesPerLine, bytes, scanLineIdx * bitmapData.Stride, width);
					}
					Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
				}
				else
				{
					Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
				}
			}
			finally
			{
				bitmap.UnlockBits(bitmapData);
			}

			return bitmap;
		}

		public static int CalculateStride(int width, KrecImagePixelFormat format)
		{
			return CalculateStride(width, format.ToPixelFormat());
		}

		public static int CalculateStride(int width, PixelFormat format)
		{
			// Stride is a H-size of an image which is rounded to 4-bytes boundary
			// See https://social.msdn.microsoft.com/Forums/vstudio/en-US/9bf9dea5-e21e-4361-a0a6-be331efde835/how-do-you-calculate-the-image-stride-for-a-bitmap?forum=csharpgeneral
			
			var bitsPerPixel = ((int) format & 0xff00) >> 8;
			var bytesPerPixel = (bitsPerPixel + 7) / 8;
			var stride = 4 * ((width * bytesPerPixel + 3) / 4);
			return stride;
		}

		public KrecImage ToGrayscaled(bool shallowCopy = false)
		{
			if (format == KrecImagePixelFormat.Format8BitPerPixel)
			{
				// The image is already in grayscaled form
				if (shallowCopy)
					return this;
				byte[] newImageData = new byte[imageData.Length];
				Array.Copy(imageData, newImageData, imageData.Length);
				return new KrecImage(width, height, bytesPerLine, 
					horizontalResolution, verticalResolution, format, newImageData);
			}

			try
			{
				return ConvertRgbColorsToGrayscale();
			}
			catch (BadImageFormatException)
			{
				return ConvertPixelFormat(PixelFormat.Format8bppIndexed);
			}
		}

		/// <summary>
		/// Converts color image (RGB or ARGB) to a grayscaled one
		/// </summary>
		/// <returns>Grayscale colors</returns>
		private KrecImage ConvertRgbColorsToGrayscale()
		{
			const int redMult = (int)(17.0 / 80.0 * 65536);
			const int greenMult = (int)(0.7154 * 65536);
			const int blueMult = (int)(0.0721 * 65536);

			var imageDataLocal = imageData;
			var stride = bytesPerLine;
			var widthLocal = width;
			var heightLocal = height;
			var newStride = CalculateStride(widthLocal, PixelFormat.Format8bppIndexed);

			var grayscaledData = new byte[newStride * heightLocal];

			if (format == KrecImagePixelFormat.Format32bppArgb ||
			    format == KrecImagePixelFormat.Format32bppRgb ||
			    format == KrecImagePixelFormat.Format32bppPArgb) //пока не трогать
			{
				for (var lineIdx = 0; lineIdx < heightLocal; lineIdx++)
				{
					for (int counter = 0, sourceIdx = lineIdx * stride, grayscaledIdx = lineIdx * newStride; 
						counter < widthLocal; 
						counter++, sourceIdx += 4, grayscaledIdx++)
					{
						grayscaledData[grayscaledIdx] = 
							(byte)((blueMult * imageDataLocal[sourceIdx] +
									greenMult * imageDataLocal[sourceIdx + 1] +
									redMult * imageDataLocal[sourceIdx + 2]) >>
									16);
					}
				}
			}
			else if (format == KrecImagePixelFormat.Format24bppRgb)
			{
				for (var lineIdx = 0; lineIdx < heightLocal; lineIdx++)
				{
					for (int counter = 0, sourceIdx = lineIdx * stride, grayscaledIdx = lineIdx * newStride;
						counter < widthLocal;
						counter++, sourceIdx += 3, grayscaledIdx++)
					{
						grayscaledData[grayscaledIdx] = 
							(byte)((blueMult * imageDataLocal[sourceIdx] +
									greenMult * imageDataLocal[sourceIdx + 1] +
						            redMult * imageDataLocal[sourceIdx + 2]) >>
						            16);
					}
				}
			}
			else
			{
				throw new BadImageFormatException("Bad pixel format");
			}

			return new KrecImage(width, height, newStride, horizontalResolution, verticalResolution, KrecImagePixelFormat.Format8BitPerPixel, grayscaledData);
		}


		/// <summary>
		/// Extracts the rectangular part of the image
		/// </summary>
		/// <param name="area">The area to extract</param>
		/// <returns>Part of image</returns>
		public KrecImage GetSubregion(Rectangle area)
		{
			return GetSubregion(area.X, area.Y, area.Width, area.Height);
		}

		/// <summary>
		/// Extracts the rectangular part of the image
		/// </summary>
		/// <param name="x">Left border of part</param>
		/// <param name="y">Top border of part</param>
		/// <param name="rectWidth">Width of part</param>
		/// <param name="rectHeight">Height of part</param>
		/// <returns>Part of image</returns>
		public KrecImage GetSubregion(int x, int y, int rectWidth, int rectHeight)
		{
			var sourceBytesPerLine = bytesPerLine;
			var bytesPerPixel = format.BytesPerPixel();

			var newBytesPerLine = KrecImage.CalculateStride(rectWidth, format);

			var newImageData = new byte[rectHeight * newBytesPerLine];

			var sourceImageData = imageData;
			var sourceIdxDelta = y * sourceBytesPerLine + x * bytesPerPixel;
			for (int rowIdx = 0, sourceIdx = sourceIdxDelta, targetIdx = 0;
				rowIdx < rectHeight;
				rowIdx++, sourceIdx += sourceBytesPerLine, targetIdx += newBytesPerLine)
			{
				Array.Copy(sourceImageData, sourceIdx, newImageData, targetIdx, rectWidth * bytesPerPixel);
			}

			return new KrecImage(rectWidth, rectHeight, newBytesPerLine,
				horizontalResolution, verticalResolution, format, newImageData);
		}


		/// <summary>
		/// Convert KrecImage to specified pixel form with standard tools. It gives bad quality
		/// </summary>
		/// <param name="newPixelFormat">New pixel format</param>
		/// <returns>Converted bitmap</returns>
		public KrecImage ConvertPixelFormat(PixelFormat newPixelFormat)
		{
			// TODO: most probably this method should be inlined in the point of use
			using (var bitmap = ToBitmap())
			{
				using (var convertedBitmap = bitmap.Clone(new Rectangle(0, 0, width, height), newPixelFormat))
				{
					return FromBitmap(convertedBitmap);
				}
			}
		}

		public void SaveToFile(string destPath)
		{
			using (var bitmap = ToBitmap())
			{
				bitmap.Save(destPath);
			}
		}
	}
}