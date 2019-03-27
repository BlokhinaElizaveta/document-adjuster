using System;

namespace Kontur.Recognition.GeometryModel.Transform
{
	public class Rotation : ITransform
	{
		private readonly double angle;

		public double Angle { get { return angle; } }

		public Rotation()
		{
			angle = 0;
		}

		public Rotation(double angle)
		{
			this.angle = angle;
		}

		public Point Transform(Point point)
		{
			var sin = Math.Sin(angle);
			var cos = Math.Cos(angle);
			var x = point.x;
			var y = point.y;
			return new Point(x * cos - y * sin, x * sin + y * cos);
		}

		public ITransform Reverse()
		{
			return new Rotation(-angle);
		}
	}
}