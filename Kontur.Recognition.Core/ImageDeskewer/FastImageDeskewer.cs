using System;
using System.Drawing;
using System.Linq;
using Kontur.Recognition.GeometryModel;
using Kontur.Recognition.ImageCore;
using Kontur.Recognition.ImageDeskewer.ImageProcessing;
using Point = System.Drawing.Point;

namespace Kontur.Recognition.ImageDeskewer
{
	/// <summary>
	/// Deskew images
	/// </summary>
	public static class FastImageDeskewer
	{
		private const int projectionSideWidth = 1024; // Side size to project in fft
		private const int maxCompletedSideWith = 2048; // Images of bigger resolution will be cuted
		private const double maxDpi = 150; // Images of bigger resolution will be compressed

		/// <summary>
		/// Get skew angle (in radians)
		/// </summary>
		/// <param name="originalBitmap">Bitmap with original image</param>
		/// <returns>Skew angle (in radians)</returns>
		public static double GetSkewAngle(Bitmap originalBitmap)
		{
			var image = KrecImage.FromBitmap(originalBitmap);
			var grayscaleImage = image.ToGrayscaled(true);
			return GetSkewAngle(grayscaleImage);
		}

		/// <summary>
		/// Get skew angle (in radians)
		/// </summary>
		/// <param name="originalImage">Original image to process</param>
		/// <returns>Skew angle (in radians)</returns>
		public static double GetSkewAngle(KrecImage originalImage)
		{
			int preComplementWidth;
			var preparedImage = PrepareImageForDeskewing(originalImage, out preComplementWidth);
			
			double angle = SkewAngleDeterminer.CalculateAngle(preparedImage);
			double fixedAngle = FixDocumentOrientation(originalImage, preparedImage, angle, preComplementWidth);
			
			return fixedAngle;
		}

		/// <summary>
		/// Calculates deskew parameters for given binarized image
		/// </summary>
		/// <param name="binarizedImage"></param>
		/// <returns></returns>
		public static DeskewParameters GetDeskewParameters(KrecImage binarizedImage)
		{
			var skewAngleRadians = GetSkewAngle(binarizedImage);
			return new DeskewParameters(binarizedImage.Width, binarizedImage.Height, -skewAngleRadians);
		}

		/// <summary>
		/// Image preparing process. Resizing and fixing quality.
		/// </summary>
		/// <param name="originalImage">Original image</param>
		/// <param name="preComplementWidth">Width used when resizing</param>
		/// <returns>Grayscaled image for descewing</returns>
		private static KrecImage PrepareImageForDeskewing(KrecImage originalImage, out int preComplementWidth)
		{
			if (originalImage.HorizontalResolution > maxDpi)
			{
				// TODO: make k be integer to avoid resampling errors 
				double k = maxDpi / originalImage.HorizontalResolution;
				originalImage = ImageSizesModifier.CompressImageToNewSizes(originalImage, (int) Math.Round(originalImage.Width * k),
					(int) Math.Round(originalImage.Height * k));
			}
			
			KrecImage projectionImage;
			preComplementWidth = Math.Min(projectionSideWidth, originalImage.Width);
			if (originalImage.Height < maxCompletedSideWith && originalImage.Width < maxCompletedSideWith) //имитирует поведение прошлого сжимателя
			{
				int dx = (maxCompletedSideWith - originalImage.Width) / 2;
				int dy = (maxCompletedSideWith - originalImage.Height) / 2;
				int newWidth = projectionSideWidth - dx;
				int newHeight = projectionSideWidth - dy;
				double k = Math.Max((double) originalImage.Height / newHeight, (double) originalImage.Width / newWidth);
				var compressedImage = ImageSizesModifier.CompressImageToNewSizes(originalImage,
					(int) (originalImage.Width / k), (int) (originalImage.Height / k));
				compressedImage = BitmapProcessor.AlignImageBackround(compressedImage);
				
				preComplementWidth = (int) (originalImage.Width / k);
				projectionImage = ImageSizesModifier.GetImageWithPowerOf2Side(compressedImage, projectionSideWidth);
			}
			else
			{
				var alignedImage = BitmapProcessor.AlignImageBackround(originalImage);
				projectionImage = ImageSizesModifier.GetImageWithPowerOf2Side(alignedImage, projectionSideWidth);
			}
			
			return projectionImage;
		}

		/// <summary>
		/// Fixing orientiation before returning scew angle
		/// </summary>
		/// <param name="originalImage">Original image</param>
		/// <param name="preparedImage">Prepared for deskewing image</param>
		/// <param name="angle">Calculated scew angle</param>
		/// <param name="preComplementWidth">Width used when resizing</param>
		/// <returns>Fixed descew angle</returns>
		private static double FixDocumentOrientation(KrecImage originalImage, KrecImage preparedImage, double angle, int preComplementWidth)
		{
			var deskewedImage = BitmapProcessor.RotateGrayscaleImage(originalImage, -angle);
			// TODO: бинаризацию можно сделать в этой точке, а не выполнять ее дважды в 
			// TODO: GetImageBordersWithoutWhiteSpaces и GetAngleToRightOrientation
			var borders = GetImageBordersWithoutWhiteSpaces(angle, preComplementWidth, deskewedImage.Width, deskewedImage.Height, preparedImage, originalImage.Width, originalImage.Height);
			
			var imagePart = deskewedImage.GetSubregion(borders);
			
			var orientationResult = OrientationDeterminer.GetAngleToRightOrientation(imagePart);
			double orientAngle = orientationResult;
			double result = angle - orientAngle;
			
			if (result > Math.PI)
				result -= 2 * Math.PI;
			else if (result < -Math.PI)
				result += 2 * Math.PI;
			
			return result;
		}

		/// <summary>
		/// Get borders of meaning part of image
		/// </summary>
		/// <param name="angle">Skew angle of image</param>
		/// <param name="completedWidth">Width to which the image was completed</param>
		/// /// <param name="resultWidth">Width of result (deskewed) image</param>
		/// <param name="resultHeight">Height of result (deskewed) image</param>
		/// <param name="projectedImage">Projected image</param>
		/// <param name="originalWidth">Width of original image</param>
		/// <param name="originalHeight">Height of original image</param>
		/// <returns>Rectangle which bounds meaning part of image</returns>
		private static Rectangle GetImageBordersWithoutWhiteSpaces(double angle, int completedWidth, int resultWidth, int resultHeight, KrecImage projectedImage, int originalWidth, int originalHeight)
		{
			var smallImage = BitmapProcessor.RotateGrayscaleImage(projectedImage, -angle); // Для скорости, границы обрезки ищем на уменьшенном изображении
			var smallBinary = BitmapProcessor.BinarizeGrayscaleImage(smallImage);
			
			var cutBorders = ImagePartitioner.GetImageWithoutBorders(smallBinary);
			
			var k = (double) originalWidth / completedWidth;
			var preComplementWidth = (int) (originalWidth / k);
			var preComplementHeight = (int) (originalHeight / k);
			
			var scaleRect = GetUnprojectedRectangle(cutBorders, projectionSideWidth, projectionSideWidth,
				preComplementWidth, preComplementHeight,
				originalWidth, originalHeight, angle);
			
			int scaleXError = resultWidth / projectionSideWidth; //Увеличиваем с учетом погрешности
			int scaleYError = resultHeight / projectionSideWidth;
			scaleRect.X = Math.Max(scaleRect.X - scaleXError, 0);
			scaleRect.Y = Math.Max(scaleRect.Y - scaleYError, 0);
			
			scaleRect.Width = Math.Min(scaleRect.Width + scaleXError, resultWidth - scaleRect.X);
			scaleRect.Height = Math.Min(scaleRect.Height + scaleYError, resultHeight - scaleRect.Y);
			return scaleRect;
		}

		/// <summary>
		/// Get coordinates of rectangle from deskewed projected image in original image
		/// </summary>
		/// <param name="projectedRectangle">Rectangle from deskewed projected image</param>
		/// <param name="projectedImageWidth">Projected image width (before deskewing)</param>
		/// <param name="projectedImageHeight">Projected image height (before deskewing)</param>
		/// <param name="preComplementImageWidth">Width of image before it was completed to power of 2</param>
		/// <param name="preComplementImageHeight">Height of image before it was completed to power of 2</param>
		/// <param name="originalImageWidth">Original image width</param>
		/// <param name="originalImageHeight">Original image Height</param>
		/// <param name="rotationAngle">Images skew angle</param>
		/// <returns>Rectangle in original image</returns>
		private static Rectangle GetUnprojectedRectangle(Rectangle projectedRectangle, int projectedImageWidth,
			int projectedImageHeight, int preComplementImageWidth, int preComplementImageHeight, int originalImageWidth,
			int originalImageHeight, double rotationAngle)
		{
			var points = new[]
			{
				new Point(projectedRectangle.Left, projectedRectangle.Top), 
				new Point(projectedRectangle.Right, projectedRectangle.Bottom)
			};
			var rotatedPoints = ImageSizesModifier.GetPointsBeforeRotation(points, -rotationAngle,
				projectedImageWidth, projectedImageHeight);
			
			var scale = (double) preComplementImageWidth / originalImageWidth;
			
			var dy = (projectedImageHeight - preComplementImageHeight) / 2;
			var dx = (projectedImageWidth - preComplementImageWidth) / 2;
			
			var descaledPoints = rotatedPoints
				.Select(p => new Point((int) ((p.X - dx) / scale), (int) ((p.Y - dy) / scale))).ToList();
			
			var result = ImageSizesModifier
				.RotatePoints(descaledPoints, -rotationAngle, originalImageWidth, originalImageHeight).ToList();
			
			return new Rectangle(result[0].X, result[0].Y, result[1].X - result[0].X, result[1].Y - result[0].Y);
		}
	}
}
