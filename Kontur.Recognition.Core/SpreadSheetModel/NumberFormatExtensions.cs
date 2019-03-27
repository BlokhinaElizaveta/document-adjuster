namespace Kontur.Recognition.SpreadSheetModel
{
	public static class NumberFormatExtensions
	{
		private const NumberFormat numberMask = NumberFormat.Number | NumberFormat.Percent | NumberFormat.Scientific | NumberFormat.Fraction | NumberFormat.Currency | NumberFormat.Defined;
		private const NumberFormat notANumberMask = NumberFormat.Date | NumberFormat.Datetime | NumberFormat.Time | NumberFormat.Logical;

		/// <summary>
		/// Checks that given set of format flags specifies a numeric value in a cell
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public static bool IsNumber(this NumberFormat format)
		{
			return ((format & numberMask) != 0);
		}

		/// <summary>
		/// Checks that given set of format flags specifies a value in a cell that must not be interpreted as a number
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public static bool IsNotANumber(this NumberFormat format)
		{
			return ((format & notANumberMask) != 0);
		}
	}
}