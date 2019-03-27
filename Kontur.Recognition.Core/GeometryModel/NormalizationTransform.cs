namespace Kontur.Recognition.GeometryModel
{
	public class NormalizationTransform
	{
		public const double a4LengthInPixels = 1170;			//~ 11,69(inch per a4) * 100(pxs per inch)

		private readonly double shiftX;
		private readonly double shiftY;
		private readonly double factor;

		public double ShiftX { get { return shiftX; } }
		public double ShiftY { get { return shiftY; } }
		public double Factor { get { return factor; } }

		public NormalizationTransform() : this(0, 0, 1) {}

		public NormalizationTransform(double shiftX, double shiftY, double factor)
		{
			this.shiftX = shiftX;
			this.shiftY = shiftY;
			this.factor = factor;
		}
	}
}