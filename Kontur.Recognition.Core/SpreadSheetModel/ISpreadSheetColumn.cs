namespace Kontur.Recognition.SpreadSheetModel
{
	public interface ISpreadSheetColumn
	{
		int StartRow { get; }
		int EndRow { get; }
		int Column { get; }
		SpreadsheetCellModel GetCell(int row);
	}
}