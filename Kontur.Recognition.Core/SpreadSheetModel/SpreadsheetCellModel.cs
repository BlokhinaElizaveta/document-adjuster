using System;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.SpreadSheetModel
{
	public class SpreadsheetCellModel
	{
		private readonly int row;
		private readonly int column;
		private readonly CellContentType contentType;
		private readonly int formatId;
		private readonly ICellFormatsProvider formatProvider;
		private readonly double value;
		private readonly string text;
		
		public SpreadsheetCellModel(int row, int column, CellContentType contentType, int formatId, [NotNull] ICellFormatsProvider formatProvider, double value, string text)
		{
			if (formatProvider == null)
			{
				throw new ArgumentException("formatProvider is null");
			}
			this.row = row;
			this.column = column;
			this.contentType = contentType;
			this.formatId = formatId;
			this.formatProvider = formatProvider;
			this.value = value;
			this.text = text;
		}

		public double Value { get { return value; } }

		public string Text { get { return text; } }

		public CellContentType ContentType { get { return contentType; } }

		public int FormatId { get { return formatId; } }

		public CellFormat Format { get { return formatProvider.GetFormat(formatId); } }

		public int Row { get { return row; } }
		public int Column { get { return column; } }

		public bool IsInRange(CellRange range)
		{
			return row >= range.StartRow && row <= range.EndRow &&
			       column >= range.StartColumn && column <= range.EndColumn;
		}
	}
}