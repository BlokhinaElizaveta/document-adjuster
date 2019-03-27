namespace Kontur.Recognition.GeometryModel
{
	public struct Point
	{
		public readonly double x;
		public readonly double y;

		public Point(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public override string ToString()
		{
			return string.Format("({0:N4}, {1:N4})", x, y);
		}
	}
}