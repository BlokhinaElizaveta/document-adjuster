using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Kontur.Recognition.SpreadSheetModel.Xml;
using Kontur.Recognition.Utils;

namespace Kontur.Recognition.SpreadSheetModel
{
	public class SpreadsheetModel : ISpreadsheetModel
	{
		private readonly NumberFormatsCollection numberFormats;

		private readonly Dictionary<CellKey, SpreadsheetCellModel> cells = new Dictionary<CellKey, SpreadsheetCellModel>();
		/// <summary>
		/// Minimal index of row in this model (inclusive!)
		/// </summary>
		private readonly int startRow;
		/// <summary>
		/// Maximal index of row in this model (inclusive!)
		/// </summary>
		private readonly int endRow;
		/// <summary>
		/// Minimal index of column in this model (inclusive!)
		/// </summary>
		private readonly int startCol;
		/// <summary>
		/// Maximal index of column in this model (inclusive!)
		/// </summary>
		private readonly int endCol;

		/// <summary>
		/// Minimal index of row in this model (inclusive!)
		/// </summary>
		public int StartRow
		{
			get { return startRow; }
		}

		/// <summary>
		/// Maximal index of row in this model (inclusive!)
		/// </summary>
		public int EndRow
		{
			get { return endRow; }
		}

		/// <summary>
		/// Minimal index of column in this model (inclusive!)
		/// </summary>
		public int StartCol
		{
			get { return startCol; }
		}

		/// <summary>
		/// Maximal index of column in this model (inclusive!)
		/// </summary>
		public int EndCol
		{
			get { return endCol; }
		}

		public SpreadsheetModel(Table table)
		{
			numberFormats = new NumberFormatsCollection(table.NumberFormats);
			startRow = table.startRow;
			endRow = table.endRow;
			startCol = table.startCol;
			endCol = table.endCol;

			if (table.Cell != null)
			{
				foreach (var xmlCell in table.Cell)
				{
					var row = xmlCell.row;
					var column = xmlCell.col;
					var cellValue = xmlCell.Value;
					var cell = new SpreadsheetCellModel(row, column, xmlCell.contentType.ToModelCellType(), xmlCell.formatId, numberFormats, xmlCell.rawValue, cellValue);
					AddCell(row, column, cell);
				}
			}
		}

		/// <summary>
		/// Restores spreadsheet model from XML representation
		/// </summary>
		/// <param name="fileName">The file to read model from</param>
		/// <returns>The reconstructed model</returns>
		/// <exception cref="InvalidOperationException">Thrown when there is a problem with restoring model from file</exception>
		public static SpreadsheetModel LoadFromFile(string fileName)
		{
			try
			{
				var serializer = new XmlSerializer(typeof(Table));
				
//				using (var istream = File.Open(fileName, FileMode.Open, FileAccess.Read))
//				{
//					var settings = new XmlReaderSettings
//					{
//						IgnoreWhitespace = true,
//					};
////					var reader = XmlReader.Create(istream, settings);
////					var reader = XmlReader.Create(istream);
//					var reader = new XmlTextReader(istream)
//					{
//						WhitespaceHandling = WhitespaceHandling.All,
//						Normalization = false,
//						XmlResolver = null
//					};
//
//					while (reader.Read())
//					{
//						Console.Out.WriteLine("Name: {0}, Type: {1}, Attr#: {2}, VType: {3}, Value: '{4}'", reader.Name, reader.NodeType, reader.AttributeCount, reader.ValueType, reader.Value);
//					}
//				}
				using (var istream = File.Open(fileName, FileMode.Open, FileAccess.Read))
				{
					//var textReader = new XmlTextReader(istream);
					var reader = XmlSecureReaderCreator.Create(istream);
					
//					var xmlTextReader = new XmlTextReader(istream)
//					                    {
//						                    WhitespaceHandling = WhitespaceHandling.All,
//						                    Normalization = false,
//						                    XmlResolver = null
//					                    };
//					var table = (Table)serializer.Deserialize(xmlTextReader);
					var table = (Table)serializer.Deserialize(reader);

					// var table = (Table)seializer.Deserialize(istream);
					return new SpreadsheetModel(table);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(string.Format("Can not  load table model from file {0} due to the following error: {1}", fileName, ex.Message), ex);
			}
		}

		internal void AddCell(int row, int column, SpreadsheetCellModel cell)
		{
			var cellKey = new CellKey {Row = row, Column = column};
			if (cells.ContainsKey(cellKey))
			{
				cells[cellKey] = cell;
			}
			else
			{
				cells.Add(cellKey, cell);
			}
		}

		public SpreadsheetCellModel GetCell(int row, int column)
		{
			SpreadsheetCellModel result;
			if (cells.TryGetValue(new CellKey {Row = row, Column = column}, out result))
			{
				return result;
			}
			return EmptyCell(row, column);
		}

		public ICellFormatsProvider GetFormatsProvider()
		{
			return numberFormats;
		}

		private SpreadsheetCellModel EmptyCell(int row, int column)
		{
			return new SpreadsheetCellModel(row, column, CellContentType.Empty, 0, numberFormats, 0, null);
		}

		private class CellKey : IEquatable<CellKey>
		{
			public int Row { get; internal set; }
			public int Column { get; internal set; }

			public bool Equals(CellKey other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return Row == other.Row && Column == other.Column;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((CellKey) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Row*397) ^ Column;
				}
			}
		}
	}
}