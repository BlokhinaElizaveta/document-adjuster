using System.Xml.Linq;

namespace Kontur.Recognition.Integration
{
	public static class HocrFormatConsts
	{
		/// <summary>
		/// Default namespace used by HOCR to markup data
		/// </summary>
		private const string strXmlNamespace = "http://www.w3.org/1999/xhtml";

		public static readonly XName XNameHtml = XName.Get("html", strXmlNamespace);
		public static readonly XName XNameHead = XName.Get("head", strXmlNamespace);
		public static readonly XName XNameTitle = XName.Get("title", strXmlNamespace);
		public static readonly XName XNameMeta = XName.Get("meta", strXmlNamespace);
		public static readonly XName XNameLink = XName.Get("link", strXmlNamespace);
		public static readonly XName XNameScript = XName.Get("script", strXmlNamespace);
		public static readonly XName XNameBody = XName.Get("body", strXmlNamespace);
		public static readonly XName XNameDiv = XName.Get("div", strXmlNamespace);
		public static readonly XName XNameP = XName.Get("p", strXmlNamespace);
		public static readonly XName XNameSpan = XName.Get("span", strXmlNamespace);
		public static readonly XName XNameStrong = XName.Get("strong", strXmlNamespace);

		public const string ClassPage = "ocr_page";
		public const string ClassTextBlock = "ocr_carea";
		public const string ClassParagraph = "ocr_par";
		public const string ClassLine = "ocr_line";
		public const string ClassWord = "ocrx_word";
		public const string ClassSeparator = "ocr_separator";
		public const string ClassTable = "ocr_table";
		public const string ClassTableCell = "ocr_cell";

		public const string AttrCellRowIndex = "row-index";
		public const string AttrCellColIndex = "col-index";
		public const string AttrCellRowSpan = "row-span";
		public const string AttrCellColSpan = "col-span";
		public const string AttrTableRowsCount = "rows-count";
		public const string AttrTableColsCount = "cols-count";
	}
}