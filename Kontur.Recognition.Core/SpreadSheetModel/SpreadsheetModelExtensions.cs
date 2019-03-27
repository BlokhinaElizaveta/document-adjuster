using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Kontur.Recognition.SpreadSheetModel.Xml;
using Kontur.Recognition.Utils.Monitor;
using CellContentTypeXml = Kontur.Recognition.SpreadSheetModel.Xml.CellContentType;
using CellFormatXml = Kontur.Recognition.SpreadSheetModel.Xml.CellFormat;
using CellXml = Kontur.Recognition.SpreadSheetModel.Xml.Cell;
using TableXml = Kontur.Recognition.SpreadSheetModel.Xml.Table;
using NumberFormatsXml = Kontur.Recognition.SpreadSheetModel.Xml.NumberFormats;

namespace Kontur.Recognition.SpreadSheetModel
{
	public static class SpreadsheetModelExtensions
	{
		public static CellRange GetCellRange(this ISpreadsheetModel model)
		{
			return new CellRange
			{
				StartRow = model.StartRow,
				StartColumn = model.StartCol,
				EndRow = model.EndRow,
				EndColumn = model.EndCol
			};
		}

		public static IEnumerable<SpreadsheetCellModel> GetRowCells(this ISpreadsheetModel model, int row)
		{
			// Maximal indices of row and column are inclusive!
			if (row < model.StartRow || row > model.EndRow)
			{
				yield break;
			}
			for (var column = model.StartCol; column <= model.EndCol; column++)
			{
				yield return model.GetCell(row, column);
			}
		}

		public static IEnumerable<SpreadsheetCellModel> GetColumnCells(this ISpreadsheetModel model, int column)
		{
			// Maximal indices of row and column are inclusive!
			if (column < model.StartCol || column > model.EndCol)
			{
				yield break;
			}
			for (var row = model.StartRow; row <= model.EndRow; row++)
			{
				yield return model.GetCell(row, column);
			}
		}

		public static IEnumerable<SpreadsheetCellModel> GetCells(this ISpreadsheetModel model, CellRange range, SpreadsheetCellScanOrder scanOrder = SpreadsheetCellScanOrder.ScanByRows)
		{
			var rangeToScan = range.Intersect(model.GetCellRange());

			if (scanOrder == SpreadsheetCellScanOrder.ScanByRows)
			{
				for (var row = rangeToScan.StartRow; row <= rangeToScan.EndRow; row++)
				{
					for (var column = rangeToScan.StartColumn; column <= rangeToScan.EndColumn; column++)
					{
						yield return model.GetCell(row, column);
					}
				}
				yield break;
			}

			if (scanOrder == SpreadsheetCellScanOrder.ScanByColumns)
			{
				for (var column = rangeToScan.StartColumn; column <= rangeToScan.EndColumn; column++)
				{
					for (var row = rangeToScan.StartRow; row <= rangeToScan.EndRow; row++)
					{
						yield return model.GetCell(row, column);
					}
				}
				yield break;
			}

			throw new InvalidOperationException("Wrong scan order");
		}

		public static ISpreadSheetRow GetRow(this ISpreadsheetModel model, int row)
		{
			return new RowImpl(model, row);
		}

		public static ISpreadSheetColumn GetColumn(this ISpreadsheetModel model, int column)
		{
			return new ColumnImpl(model, column);
		}

		public static void SaveModel(this ISpreadsheetModel model, string outputFile)
		{
			var table = ExtractModelData(model, new ProgressMonitorFake());
			var serializer = new XmlSerializer(table.GetType());
			using (var outStream = File.Open(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				using (var sw = new StreamWriter(outStream, Encoding.UTF8))
				{
					serializer.Serialize(sw, table);
				}
			}
		}

		private static Table ExtractModelData(this ISpreadsheetModel model, IProgressMonitor monitor)
		{
			var usedArea = model.GetCellRange();
			var cellList = new List<CellXml>((usedArea.RowsCount + 1) * (usedArea.ColumnsCount + 1));
			var formatsList = new List<CellFormatXml>();
			var formatsUsed = new HashSet<int>();

			monitor.AddStepsCount(usedArea.EndRow - usedArea.StartRow);

			//Console.Out.WriteLine("Rows: {0}, Columns: {1}", usedArea.RowsCount, usedArea.ColumnsCount);
			var cellFormatsProvider = model.GetFormatsProvider();
			for (var row = usedArea.StartRow; row <= usedArea.EndRow; row++)
			{
				for (var column = usedArea.StartColumn; column <= usedArea.EndColumn; column++)
				{
					var cell = model.GetCell(row, column);
					if (cell.ContentType != CellContentType.Empty)
					{
						var formatId = cell.FormatId;
						if (!formatsUsed.Contains(formatId))
						{
							var format = cellFormatsProvider.GetFormat(cell.FormatId);
							formatsUsed.Add(formatId);
							var formatInfo = new CellFormatXml
							{
								id = format.FormatId,
								type = (int)format.FormatTypeFlags,
								formatString = format.FormatString,
							};

							formatsList.Add(formatInfo);
						}
						var resultCell = new CellXml
						{
							row = row,
							col = column,
							rawValue = cell.Value,
							Value = cell.Text,
							formatId = cell.FormatId,
							contentType = cell.ContentType.ToXmlCellType(),
							xMin = 0,
							xMax = 0,
							yMin = 0,
							yMax = 0,
						};
						cellList.Add(resultCell);
					}
				}
				monitor.StepFinished();
			}

			return new TableXml
			{
				startRow = usedArea.StartRow,
				endRow = usedArea.EndRow,
				startCol = usedArea.StartColumn,
				endCol = usedArea.EndColumn,
				NumberFormats = new NumberFormatsXml
				{
					CellFormat = formatsList.ToArray(),
					defaultDecimals = cellFormatsProvider.StandardMaxDecimals,
					nullDate = cellFormatsProvider.NullDate,
				},
				Cell = cellList.ToArray(),
			};
		}


		private class RowImpl : ISpreadSheetRow
		{
			private readonly ISpreadsheetModel spreadSheet;
			private readonly int row;

			public RowImpl(ISpreadsheetModel spreadSheet, int row)
			{
				this.spreadSheet = spreadSheet;
				this.row = row;
			}

			public int StartCol { get { return spreadSheet.StartCol; } }
			public int EndCol { get { return spreadSheet.EndCol; } }
			public int Row { get { return row; } }

			public SpreadsheetCellModel GetCell(int column)
			{
				return spreadSheet.GetCell(row, column);
			}
		}

		private class ColumnImpl : ISpreadSheetColumn
		{
			private readonly ISpreadsheetModel spreadSheet;
			private readonly int column;

			public ColumnImpl(ISpreadsheetModel spreadSheet, int column)
			{
				this.spreadSheet = spreadSheet;
				this.column = column;
			}

			public int StartRow { get { return spreadSheet.StartRow; } }
			public int EndRow { get { return spreadSheet.EndRow; } }
			public int Column { get { return column; } }

			public SpreadsheetCellModel GetCell(int row)
			{
				return spreadSheet.GetCell(row, column);
			}
		}

	}
}