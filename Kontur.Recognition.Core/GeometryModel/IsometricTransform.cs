using System;
using System.IO;

namespace Kontur.Recognition.GeometryModel
{
	public class IsometricTransform
	{
		private readonly double angleRadians;
		private readonly double postShiftX;
		private readonly double postShiftY;
		private readonly double preShiftX;
		private readonly double preShiftY;
		private double sinAngle;
		private double cosAngle;
		
		public double SinAngle { get { return sinAngle; } }
		public double CosAngle { get { return cosAngle; } }
		public double AngleRadians { get { return angleRadians; } }

		public IsometricTransform(double angleRadians, double postShiftX, double postShiftY, double preShiftX, double preShiftY)
		{
			this.angleRadians = angleRadians;
			this.postShiftX = postShiftX;
			this.postShiftY = postShiftY;
			this.preShiftX = preShiftX;
			this.preShiftY = preShiftY;
			PostInit();
		}

		public IsometricTransform(double angle, Point postShift)
			: this(angle, postShift.x, postShift.y, 0, 0)
		{
		}

		private IsometricTransform(double angle, Point postShift, Point preShift)
			:this(angle, postShift.x, postShift.y, preShift.x, preShift.y)
		{
		}

		public IsometricTransform(BinaryReader reader)
		{
			angleRadians = reader.ReadDouble();
			postShiftX = reader.ReadDouble();
			postShiftY = reader.ReadDouble();
			preShiftX = reader.ReadDouble();
			preShiftY = reader.ReadDouble();
			PostInit();
		}
		
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(angleRadians);
			writer.Write(postShiftX);
			writer.Write(postShiftY);
			writer.Write(preShiftX);
			writer.Write(preShiftY);
		}

		private void PostInit()
		{
			sinAngle = Math.Sin(angleRadians);
			cosAngle = Math.Cos(angleRadians);
		}

		/// <summary>
		/// Calculates composition of two transformations, i.e. new transformation C(x) = A(B(x))
		/// </summary>
		/// <param name="trA"></param>
		/// <param name="trB"></param>
		/// <returns></returns>
		public static IsometricTransform Compose(IsometricTransform trA, IsometricTransform trB)
		{
			var newAngle = Angles.NormalizeRad(trA.AngleRadians + trB.AngleRadians);
			var newPreShift = new Point(trB.preShiftX, trB.preShiftY);
			var newPostShift = trA.Transform(new Point(trB.postShiftX, trB.postShiftY));
			return new IsometricTransform(newAngle, newPostShift, newPreShift);
		}

		public static IsometricTransform DetectTransform(Func<Point, Point> transform)
		{
			var pntZeroImage = transform(new Point(0, 0));
			var pntTestImage = transform(new Point(1000, 1000));
			var cosPlusSin = (pntTestImage.x - pntZeroImage.x) / 1000;
			var cosMinusSin = (pntTestImage.y - pntZeroImage.y) / 1000;

			var cosAngle = (cosPlusSin + cosMinusSin) / 2;
			var sinAngle = (cosPlusSin - cosMinusSin) / 2;
			var angle = Angles.DecodeAngle(cosAngle, sinAngle);
			return new IsometricTransform(angle, pntZeroImage);
		}

		public static IsometricTransform GetIdentityTransform()
		{
			return new IsometricTransform(0, 0, 0, 0, 0);
		}

		public IsometricTransform Reverse()
		{
			return new IsometricTransform(-angleRadians, -preShiftX, -preShiftY, -postShiftX, -postShiftY);
		}

		public Point Transform(Point src)
		{
			var srcX = src.x + preShiftX;
			var srcY = src.y + preShiftY;
			return new Point(srcX * cosAngle - srcY * sinAngle + postShiftX, srcX * sinAngle + srcY * cosAngle + postShiftY);
		}

		public override string ToString()
		{
			return string.Format("Pre: ({0:N2}, {1:N2}), Angle: {2:N2}, Post: ({3:N2}, {4:N2})", preShiftX, preShiftY, Angles.RadToDeg(angleRadians), postShiftX, postShiftY);
		}

	}
}