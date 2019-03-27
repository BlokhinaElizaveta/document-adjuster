using System;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.SpreadSheetModel
{
	/// <summary>
	/// Represents rectangular range of cells in spreadsheet
	/// Rows and columns indices are 0-based. Both start end end indices are inclusive
	/// </summary>
	public class CellRange
	{
		public int StartRow { get; set; }
		public int StartColumn { get; set; }
		public int EndRow { get; set; }
		public int EndColumn { get; set; }

		public int RowsCount
		{
			get { return EndRow - StartRow + 1; }
		}

		public int ColumnsCount
		{
			get { return EndColumn - StartColumn + 1; }
		}

		public override string ToString()
		{
			return string.Format("StartRow: {0}, StartColumn: {1}, EndRow: {2}, EndColumn: {3}", StartRow, StartColumn, EndRow, EndColumn);
		}

		[UsedImplicitly]
		public CellRange Duplicate()
		{
			return (CellRange)MemberwiseClone();
		}

		public CellRange Intersect(CellRange range)
		{
			return new CellRange
				{
					StartRow = Math.Max(StartRow, range.StartRow),
					StartColumn = Math.Max(StartColumn, range.StartColumn),
					EndRow = Math.Min(EndRow, range.EndRow),
					EndColumn = Math.Min(EndColumn, range.EndColumn)
				};
		}
	}
}