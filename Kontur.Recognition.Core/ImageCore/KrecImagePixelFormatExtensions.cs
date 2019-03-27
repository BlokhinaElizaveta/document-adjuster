using System;
using System.Drawing.Imaging;

namespace Kontur.Recognition.ImageCore
{
	public static class KrecImagePixelFormatExtensions
	{
		/// <summary>
		/// Returns number of bytes needed to store one pixel in the specified format
		/// </summary>
		/// <param name="pixelFormat"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static int BytesPerPixel(this KrecImagePixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case KrecImagePixelFormat.Format1BitPerPixel:
				case KrecImagePixelFormat.Format8BitPerPixel: // intentionally pass through
					return 1;
				case KrecImagePixelFormat.Format24bppRgb:
					return 3;
				case KrecImagePixelFormat.Format32bppArgb:
				case KrecImagePixelFormat.Format32bppRgb:     // intentionally pass through
				case KrecImagePixelFormat.Format32bppPArgb:   // intentionally pass through
					return 4;
				default:
					throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null);
			}
		}

		/// <summary>
		/// Returns number of bits needed to store one pixel in the specified format
		/// </summary>
		/// <param name="pixelFormat"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static int BitsPerPixel(this KrecImagePixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case KrecImagePixelFormat.Format1BitPerPixel:
					return 1;
				case KrecImagePixelFormat.Format8BitPerPixel:
					return 8;
				case KrecImagePixelFormat.Format24bppRgb:
					return 24;
				case KrecImagePixelFormat.Format32bppArgb:
				case KrecImagePixelFormat.Format32bppRgb:       // intentionally pass through
				case KrecImagePixelFormat.Format32bppPArgb:     // intentionally pass through
					return 32;
				default:
					throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null);
			}
		}

		/// <summary>
		/// Converts pixel format to standard System.Drawing PixelFormat enumeration
		/// </summary>
		/// <param name="pixelFormat"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static PixelFormat ToPixelFormat(this KrecImagePixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case KrecImagePixelFormat.Format1BitPerPixel:
					return PixelFormat.Format1bppIndexed;
				case KrecImagePixelFormat.Format8BitPerPixel:
					return PixelFormat.Format8bppIndexed;
				case KrecImagePixelFormat.Format24bppRgb:
					return PixelFormat.Format24bppRgb;
				case KrecImagePixelFormat.Format32bppArgb:
					return PixelFormat.Format32bppArgb;
				case KrecImagePixelFormat.Format32bppRgb:
					return PixelFormat.Format32bppRgb;
				case KrecImagePixelFormat.Format32bppPArgb:
					return PixelFormat.Format32bppArgb;
				default:
					throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null);
			}
		}
	}
}