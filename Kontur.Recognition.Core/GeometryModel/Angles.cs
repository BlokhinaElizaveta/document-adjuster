using System;

namespace Kontur.Recognition.GeometryModel
{
	public static class Angles
	{
		private const double radToDegMultiplier = 180/Math.PI;

		public static double RadToDeg(double angleInRadians)
		{
			return angleInRadians*radToDegMultiplier;
		}

		public static double DegToRad(double angleInDegrees)
		{
			return angleInDegrees/radToDegMultiplier;
		}

		/// <summary>
		/// Normalizes given value to make it fitting into range (-PI, PI]
		/// </summary>
		/// <param name="d"></param>
		public static double NormalizeRad(double d)
		{
			const double pi2 = 2*Math.PI;
			while (d > Math.PI)
			{
				d -= pi2;
			}
			while (d <= -Math.PI)
			{
				d += pi2;
			}
			return d;
		}

		public static double DecodeAngle(double cosAngle, double sinAngle)
		{
			if (sinAngle >= 0)
			{
				return (cosAngle >= 0) ? Math.Asin(sinAngle) : Math.PI - Math.Asin(sinAngle);
			}
			return (cosAngle >= 0) ? Math.Asin(sinAngle) : -Math.PI - Math.Asin(sinAngle);
		}

	}
}
