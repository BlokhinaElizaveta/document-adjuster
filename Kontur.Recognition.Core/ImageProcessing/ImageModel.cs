using System;
using Kontur.Recognition.GeometryModel;

namespace Kontur.Recognition.ImageProcessing
{
	public class ImageModel
	{
		private const int mmInInch = 254;

		public BoundingBox ImageBox { get; private set; }
		/// <summary>
		/// Horizontal resolution of the image (can be different from the vertical one)
		/// </summary>
		public double? ResolutionX { get; private set; }
		/// <summary>
		/// Vertical resolution of the image (can be different from the horizontal one)
		/// </summary>
		public double? ResolutionY { get; private set; }
		public ResolutionUnit ResolutionUnit { get; private set; }
		/// <summary>
		/// This the number of bits in a color sample within a pixel
		/// </summary>
		public ColorDepth ColorDepth { get; private set; }
		/// <summary>
		/// Defines image color space type 
		/// </summary>
		public ColorSpace ColorSpace { get; private set; }


		public int? ResolutionXInDPI
		{
			get { return toDPI(ResolutionX, ResolutionUnit); }
		}

		public int? ResolutionYInDPI
		{
			get { return toDPI(ResolutionY, ResolutionUnit); }
		}

		public static int? toDPI(double? sourceValue, ResolutionUnit sourceUnit)
		{
			if (sourceValue == null)
				return null;
			switch (sourceUnit)
			{
				case ResolutionUnit.PixelsPerInch:
					return (int)Math.Round(sourceValue.Value);
				case ResolutionUnit.PixelsPerCentimeter:
					return (int)Math.Round(sourceValue.Value * mmInInch / 100.0);
				case  ResolutionUnit.Unknown:
					// source resolution is not known. So value is just preserved.
					return (int)Math.Round(sourceValue.Value);
				default:
					throw new ArgumentException("Bad source resolution unit");
			}
		}

		public static int? toDPCm(double? sourceValue, ResolutionUnit sourceUnit)
		{
			if (sourceValue == null)
				return null;
			switch (sourceUnit)
			{
				case ResolutionUnit.PixelsPerInch:
					return (int)Math.Round(sourceValue.Value * 100.0 / mmInInch);
				case ResolutionUnit.PixelsPerCentimeter:
					return (int)Math.Round(sourceValue.Value);
				case  ResolutionUnit.Unknown:
					// source resolution is not known. So value is just preserved.
					return (int)Math.Round(sourceValue.Value);
				default:
					throw new ArgumentException("Bad source resolution unit");
			}
		}

		public ImageModel(BoundingBox imageBox, int? resolution, ResolutionUnit resolultionUnit)
			: this(imageBox, resolution, resolution, resolultionUnit)
		{
		}

		public ImageModel(BoundingBox imageBox, double? resolutionX, double? resolutionY, ResolutionUnit resolultionUnit)
		{
			ImageBox = imageBox;
			ResolutionX = resolutionX;
			ResolutionY = resolutionY;
			ResolutionUnit = resolultionUnit;
		}

		public ImageModel(BoundingBox imageBox, double? resolutionX, double? resolutionY, ResolutionUnit resolultionUnit, ColorDepth colorDepth, ColorSpace colorSpace)
			: this(imageBox, resolutionX, resolutionY, resolultionUnit)
		{
			ColorDepth = colorDepth;
			ColorSpace = colorSpace;
		}
	}
}