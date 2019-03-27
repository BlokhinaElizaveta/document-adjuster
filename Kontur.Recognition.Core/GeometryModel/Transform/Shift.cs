namespace Kontur.Recognition.GeometryModel.Transform
{
	public class Shift : ITransform
	{
		private readonly double shiftX;
		private readonly double shiftY;

		public double ShiftX { get { return shiftX; } }
		public double ShiftY { get { return shiftY; } }

		public Shift()
		{
			shiftX = 0;
			shiftY = 0;
		}

		public Shift(double shiftX, double shiftY)
		{
			this.shiftX = shiftX;
			this.shiftY = shiftY;
		}

		public Point Transform(Point point)
		{
			return new Point(point.x + shiftX, point.y + shiftY);
		}

		public ITransform Reverse()
		{
			return new Shift(-shiftX, -shiftY);
		}
	}
}