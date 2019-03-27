namespace Kontur.Recognition.GeometryModel.Transform
{
	public class Scale : ITransform
	{
		private readonly double factor;

		public double Factor { get { return factor; } }

		public Scale()
		{
			factor = 1;
		}

		public Scale(double factor)
		{
			this.factor = factor;
		}

		public Point Transform(Point point)
		{
			return new Point(point.x * factor, point.y * factor);
		}

		public ITransform Reverse()
		{
			return (factor == 0) ? new Scale() : new Scale(1 / factor);
		}
	}
}