namespace Kontur.Recognition.SpreadSheetModel
{
	public enum SpreadsheetCellScanOrder
	{
		/// <summary>
		/// Specifies scan order when cells in spreadsheet are scanned in row by row order (in order of row index increase) 
		/// and then cells in the row are enumerated in order of column index increase
		/// </summary>
		ScanByRows,
		/// <summary>
		/// Specifies scan order when cells in spreadsheet are scanned in column by column order (in order of column index increase) 
		/// and then cells in the column are enumerated in order of row index increase
		/// </summary>
		ScanByColumns
	}
}