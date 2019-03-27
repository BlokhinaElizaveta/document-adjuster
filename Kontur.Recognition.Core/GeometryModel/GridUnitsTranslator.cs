using System;
using System.ComponentModel;

namespace Kontur.Recognition.GeometryModel
{
	public class GridUnitsTranslator
	{
		private readonly RoundMode roundMode;
		private readonly double multiplier;

		public static GridUnitsTranslator GetUnitsTranslator(GridUnit sourceUnit, GridUnit targetUnit, RoundMode roundMode = RoundMode.Round)
		{
			return new GridUnitsTranslator(sourceUnit, targetUnit, roundMode);
		}
			
		public GridUnitsTranslator(GridUnit sourceUnit, GridUnit targetUnit, RoundMode roundMode = RoundMode.Round)
		{
			this.roundMode = roundMode;
			multiplier = (double)targetUnit.Divisor / sourceUnit.Divisor;
		}

		public double Translate(double value)
		{
			return value * multiplier;
		}

		public int TranslateToGrid(double value)
		{
			return TranslateToGrid(value, roundMode);
		}

		public int TranslateToGrid(double value, RoundMode roundingMode)
		{
			var result = Translate(value);
			switch (roundingMode)
			{
				case RoundMode.Round:
				{
					return (int)Math.Round(result);
				}
				case RoundMode.Ceiling:
				{
					return (int)Math.Ceiling(result);
				}
				case RoundMode.Floor:
				{
					return (int)Math.Floor(result);
				}
				case RoundMode.Truncate:
				{
					return (int)Math.Truncate(result);
				}
			}
			throw new InvalidEnumArgumentException();
		}
	}
}