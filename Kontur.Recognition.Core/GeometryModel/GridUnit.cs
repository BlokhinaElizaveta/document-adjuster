using System;

namespace Kontur.Recognition.GeometryModel
{
	/// <summary>
	/// Represents relation between units used in geometric model and physical length units:
	/// it specifies resolution of geometric model grid cell relative to 1 inch (i.e. in DPI). 
	/// For example, GridUnit(300) represents grid cell size of 1/300 of inch (300 DPI)
	/// </summary>
	public class GridUnit : IEquatable<GridUnit>
	{
		// PDF stores coordinates with relation to the grid with the step of 1/72 of inch.
		// To preserve precision and stay on integer grid we will use these native PDF coordinates multiplied by 10^5.
		public static readonly GridUnit PDF_UNITS = new GridUnit(7200000);

		public static readonly GridUnit DPI_300 = new GridUnit(300);
		public static readonly GridUnit DPI_150 = new GridUnit(150);
		public static readonly GridUnit DPI_75 = new GridUnit(75);
		public static readonly GridUnit DPI_72 = new GridUnit(72);

		/// <summary>
		/// Special kind of units which may be used in cases when resolution of grid is not specified.
		/// </summary>
		public static readonly GridUnit UNKNOWN_UNITS = new GridUnit(0);

		public static GridUnit ByResolution(int resolution)
		{
			switch (resolution)
			{
				case 300:
				{
					return DPI_300;
				}
				case 150:
				{
					return DPI_150;
				}
				case 75:
				{
					return DPI_75;
				}
			}
			return new GridUnit(resolution);
		}
		 
		private readonly int divisor;

		public int Divisor { get { return divisor; } }

		private GridUnit(int divisor)
		{
			this.divisor = divisor;
		}

		public bool Equals(GridUnit other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return divisor == other.divisor;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((GridUnit) obj);
		}

		public override int GetHashCode()
		{
			return divisor;
		}
	}
}