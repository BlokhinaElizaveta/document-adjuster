using System;

namespace Kontur.Recognition.GeometryModel
{
	/// <summary>
	/// Holds coordinates of bounding rectangle at PDF page.
	/// Coordinates are represented in given units, so different instances can use different grid units to store box size
	/// </summary>
	public class BoundingBox 
	{
		private readonly int xMin;
		private readonly int yMin;
		private readonly int xMax;
		private readonly int yMax;
		//private readonly GridUnit gridUnit;

		public int XMin { get { return xMin; }  }
		public int YMin { get { return yMin; }  }
		public int XMax { get { return xMax; }  }
		public int YMax { get { return yMax; }  }
		public int Height { get { return yMax - yMin; } }
		public int Width { get { return xMax - xMin; } }
		public int CenterX { get { return (xMin + xMax) / 2; } }
		public int CenterY { get { return (yMin + yMax) / 2; } }
		//public GridUnit GridUnit { get { return gridUnit; } }

		public BoundingBox(int xMin, int yMin, int xMax, int yMax)
		{
			if (xMax < xMin)
			{
				throw new ArgumentException(string.Format("Value of xMin ({0}) must be less than value of xMax ({1})", xMin, xMax));
			}
			if (yMax < yMin)
			{
				throw new ArgumentException(string.Format("Value of yMin ({0}) must be less than value of yMax ({1})", xMin, xMax));
			}
			this.xMin = xMin;
			this.yMin = yMin;
			this.xMax = xMax;
			this.yMax = yMax;
			//this.gridUnit = gridUnit;
		}

		public static BoundingBox Union(params BoundingBox[] boxes)
		{
			if (boxes.Length == 0)
			{
				return new BoundingBox(0,0,0,0);
			}

			var xMin = Int32.MaxValue;
			var yMin = Int32.MaxValue;
			var xMax = Int32.MinValue;
			var yMax = Int32.MinValue;
			foreach (var bbox in boxes)
			{
				xMin = (bbox.xMin < xMin) ? bbox.xMin : xMin;
				yMin = (bbox.yMin < yMin) ? bbox.yMin : yMin;
				xMax = (bbox.xMax > xMax) ? bbox.xMax : xMax;
				yMax = (bbox.yMax > yMax) ? bbox.yMax : yMax;
			}
			return new BoundingBox(xMin, yMin, xMax, yMax);
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}) - ({2}, {3})", xMin, yMin, xMax, yMax);
		}

		/// <summary>
		/// Returns whether this element occupies any space (i.e. both height and width are non-zero)
		/// </summary>
		/// <returns></returns>
		public bool IsEmpty()
		{
			return Height == 0 || Width == 0;
		}
	}
}