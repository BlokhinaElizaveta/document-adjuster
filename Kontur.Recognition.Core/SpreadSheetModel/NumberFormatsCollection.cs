using System;
using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using CellFormatXml = Kontur.Recognition.SpreadSheetModel.Xml.CellFormat;
using TableXml = Kontur.Recognition.SpreadSheetModel.Xml.Table;
using NumberFormatsXml = Kontur.Recognition.SpreadSheetModel.Xml.NumberFormats;

namespace Kontur.Recognition.SpreadSheetModel
{
	public class NumberFormatsCollection : ICellFormatsProvider
	{
		private readonly DateTime nullDate;
		private readonly int standardMaxDecimals;
		private Func<int, CellFormat> retrieveFormat = i => null;

		/// <summary>
		/// Returns date which corresponds to value 0 of cell value
		/// </summary>
		public DateTime NullDate
		{
			get { return nullDate; }
		}

		/// <summary>
		/// Maximal number of decimal digits for default cell format
		/// </summary>
		public int StandardMaxDecimals
		{
			get { return standardMaxDecimals; }
		}

		[NotNull]
		private Dictionary<int, CellFormat> cellFormats = new Dictionary<int, CellFormat>();

		public NumberFormatsCollection([NotNull] NumberFormatsXml formats)
			:this(formats.defaultDecimals, formats.nullDate)
		{
			if (formats.CellFormat != null)
			{
				PopulateFormats(formats.CellFormat.Select(fmt => new CellFormat(fmt.id, fmt.formatString, (NumberFormat)fmt.type)));
			}
		}

		public NumberFormatsCollection([NotNull] IEnumerable<CellFormat> formats, int defaultDecimals, DateTime nullDate)
			:this(defaultDecimals, nullDate)
		{
			PopulateFormats(formats);
		}

		public NumberFormatsCollection(int defaultMaxDecimals, DateTime nullDateValue)
		{
			nullDate = new DateTime(nullDateValue.Year, nullDateValue.Month, nullDateValue.Day);
			standardMaxDecimals = defaultMaxDecimals;
		}

		protected void SetFormatRetriever(Func<int, CellFormat> retriever)
		{
			retrieveFormat = retriever;
		}

		public CellFormat GetFormat(int formatId)
		{
			CellFormat result;
			if (cellFormats.TryGetValue(formatId, out result))
			{
				return result;
			}
			// Cache missed. Retrieving format from format provider
			lock (cellFormats)
			{
				if (!cellFormats.TryGetValue(formatId, out result))
				{
					result = retrieveFormat(formatId);
					if (result != null)
					{
						var newDict = new Dictionary<int, CellFormat>(cellFormats) {{formatId, result}};
						cellFormats = newDict;
					}
				}
			}
			return result;
		}

		protected void PopulateFormats(IEnumerable<CellFormat> formats)
		{
			lock (cellFormats)
			{
				var newDict = new Dictionary<int, CellFormat>(cellFormats);
				foreach (var format in formats)
				{
					var formatId = format.FormatId;
					CellFormat existingFormat;
					if (!newDict.TryGetValue(formatId, out existingFormat))
					{
						newDict.Add(formatId, format);
					}
					else
					{
						throw new ArgumentException(string.Format("Formats with duplicate key {0} are detected", formatId));
					}
				}
				cellFormats = newDict;
			}
		}
	}
}