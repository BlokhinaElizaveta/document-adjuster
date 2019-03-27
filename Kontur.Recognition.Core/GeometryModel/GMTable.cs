using System.Collections.Generic;
using System.Linq;

namespace Kontur.Recognition.GeometryModel
{
	public class GMTable : GMElement
	{
		private readonly GMTableCell[,] cells;

		public GMTable(BoundingBox boundingBox, int rowsCount, int columnsCount, IEnumerable<GMTableCell> cells)
			: base(boundingBox)
		{
			this.cells =  new GMTableCell[rowsCount, columnsCount];
			foreach (var cell in cells)
				this.cells[cell.RowIndex, cell.ColIndex] = cell;
		}

		public GMTable(BoundingBox boundingBox, int[] rows, int[] columns, IEnumerable<GMTextBlock> textBlocks)
			: this(boundingBox, rows.Length - 1, columns.Length - 1, textBlocks.Select(tb => CreateTableCell(tb, rows, columns)))
		{
		}

		public int RowsCount { get { return cells.GetLength(0); } }
		public int ColsCount { get { return cells.GetLength(1); } }

		public IEnumerable<GMTableCell> Cells()
		{
			return cells.Cast<GMTableCell>().Where(c => c != null);
		}

		private static GMTableCell CreateTableCell(GMTextBlock textBlock, int[] hSeparators, int[] vSeparators)
		{
			var box = textBlock.BoundingBox;
			int rowIndex = hSeparators.Count(y => y <= box.YMin) - 1;
			int rowSpan = hSeparators.Count(y => y <= box.YMax) - rowIndex - 1;
			int colIndex = vSeparators.Count(x => x <= box.XMin) - 1;
			int colSpan = vSeparators.Count(x => x <= box.XMax) - colIndex - 1;
			return new GMTableCell(textBlock, rowIndex, colIndex, rowSpan, colSpan);
		}
	}
}