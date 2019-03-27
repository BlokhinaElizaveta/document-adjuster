using System;
using CellContentTypeXml = Kontur.Recognition.SpreadSheetModel.Xml.CellContentType;
using CellContentTypeModel = Kontur.Recognition.SpreadSheetModel.CellContentType;

namespace Kontur.Recognition.SpreadSheetModel.Xml
{
	public static class SpreadsheetXmlExtensions
	{
		public static CellContentTypeXml ToXmlCellType(this CellContentTypeModel contentType)
		{
			switch (contentType)
			{
				case CellContentTypeModel.Empty:
					return CellContentTypeXml.empty;
				case CellContentTypeModel.Text:
					return CellContentTypeXml.text;
				case CellContentTypeModel.Value:
					return CellContentTypeXml.value;
				case CellContentTypeModel.Formula:
					return CellContentTypeXml.text;	// intentionally mapped to text
				default:
					throw new ArgumentException("unknown content type");
			}
		}

		public static CellContentTypeModel ToModelCellType(this CellContentTypeXml contentType)
		{
			switch (contentType)
			{
				case CellContentTypeXml.empty:
					return CellContentTypeModel.Empty;
				case CellContentTypeXml.text:
					return CellContentTypeModel.Text;
				case CellContentTypeXml.value:
					return CellContentTypeModel.Value;
				default:
					throw new ArgumentException("unknown content type");
			}
		}
	}
}
