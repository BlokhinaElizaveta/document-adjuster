namespace Kontur.Recognition.SpreadSheetModel
{
	public interface ISpreadsheetModel 
	{
		/// <summary>
		/// Minimal index of row in this model (0-based, inclusive!)
		/// </summary>
		int StartRow { get; }

		/// <summary>
		/// Maximal index of row in this model (0-based, inclusive!)
		/// </summary>
		int EndRow { get; }

		/// <summary>
		/// Minimal index of column in this model (0-based, inclusive!)
		/// </summary>
		int StartCol { get; }

		/// <summary>
		/// Maximal index of column in this model (0-based, inclusive!)
		/// </summary>
		int EndCol { get; }

		SpreadsheetCellModel GetCell(int row, int column);

		/// <summary>
		/// Returns service which resolves cell formats present in this model
		/// </summary>
		/// <returns></returns>
		ICellFormatsProvider GetFormatsProvider();
	}
}