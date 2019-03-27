namespace Kontur.Recognition.SpreadSheetModel
{
	public interface ISpreadSheetRow
	{
		/// <summary>
		/// Minimal index of column in this row (0-based, inclusive!)
		/// </summary>
		int StartCol { get; }

		/// <summary>
		/// Maximal index of column in this row (0-based, inclusive!)
		/// </summary>
		int EndCol { get; }

		/// <summary>
		/// Index of row in this model (0-based, inclusive!)
		/// </summary>
		int Row { get; }

		SpreadsheetCellModel GetCell(int column);
	}
}