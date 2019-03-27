using System;
using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	/// <summary>
	/// Holds parameters of desckew transformation 
	/// Coordinates are represented in integer grid units 
	/// </summary>
	public class DeskewParameters
	{
		private readonly int sourceHeight;
		private readonly int sourceWidth;
		private readonly double angleRadians; // in radians

		[NotNull] private IsometricTransform transform;
		private int targetHeight;
		private int targetWidth;

		public int SourceHeight { get { return sourceHeight; } }
		public int SourceWidth { get { return sourceWidth; } }

		public double AngleRadians { get { return angleRadians; } }

		[NotNull]
		public IsometricTransform Transform { get { return transform; } }

		public int TargetHeight { get { return targetHeight; } }
		public int TargetWidth { get { return targetWidth; } }

		public DeskewParameters(int sourceWidth, int sourceHeight, double angleRadians)
		{
			this.sourceHeight = sourceHeight;
			this.sourceWidth = sourceWidth;
			this.angleRadians = angleRadians;
			CalculateTransformationParams();
		}

		private void CalculateTransformationParams()
		{
			var rotation = new IsometricTransform(angleRadians, 0, 0, 0, 0);
			var points = new List<Point>
			{
				rotation.Transform(new Point(0, 0)),
				rotation.Transform(new Point(0, sourceHeight)),
				rotation.Transform(new Point(sourceWidth, 0)),
				rotation.Transform(new Point(sourceWidth, sourceHeight))
			};
			var maxX = points.Select(point => point.x).Max();
			var minX = points.Select(point => point.x).Min();
			var maxY = points.Select(point => point.y).Max();
			var minY = points.Select(point => point.y).Min();
			transform = new IsometricTransform(angleRadians, -minX, -minY, 0, 0);
			targetWidth = (int) Math.Round(maxX - minX);
			targetHeight = (int)Math.Round(maxY - minY);
		}
		
		public override string ToString()
		{
			return string.Format("Source: {0}x{1}, Target: {2}x{3} Transform: {4}", sourceWidth, sourceHeight, targetWidth, targetHeight, transform);
		}
	}
}