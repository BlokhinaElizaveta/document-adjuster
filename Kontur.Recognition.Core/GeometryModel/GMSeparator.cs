using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	public class GMSeparator : GMElement
	{
		private readonly int startPointX;
		private readonly int startPointY;
		private readonly int endPointX;
		private readonly int endPointY;
		private readonly int width;

		public int StartPointX { get { return startPointX; } }
		public int StartPointY { get { return startPointY; } }
		public int EndPointX { get { return endPointX; } }
		public int EndPointY { get { return endPointY; } }
		public int Width { get { return width; } }

		public GMSeparator([NotNull] BoundingBox boundingBox, int startPointX, int startPointY, int endPointX, int endPointY, int width)
			: base(boundingBox)
		{
			this.startPointX = startPointX;
			this.startPointY = startPointY;
			this.endPointX = endPointX;
			this.endPointY = endPointY;
			this.width = width;
		}
	}
}