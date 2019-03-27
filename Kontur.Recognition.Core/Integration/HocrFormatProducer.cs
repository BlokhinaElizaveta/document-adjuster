using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Kontur.Recognition.GeometryModel;
using Kontur.Recognition.Utils;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Integration
{
	/// <summary>
	/// This class implement logic to produce HOCR output data for given geometric model
	/// <p>HOCR format is XHTML, which has a nice look in browsers if folowing files exist in the same folder: hocr.css, hocr.js è jquery.js.
	/// All this files can be found in this project in the same folder as this file.</p>
	/// </summary>
	public static class HocrFormatProducer
	{
		public static XDocument ProcessModel([NotNull] TextGeometryModel model, string pathToScripts = "")
		{
			var bodyElement = new XElement(HocrFormatConsts.XNameBody);
			var document = 
				new XDocument(
					new XElement(HocrFormatConsts.XNameHtml,
						new XElement(HocrFormatConsts.XNameHead, 
							new XElement(HocrFormatConsts.XNameTitle, "text geometric model"),
							new XElement(HocrFormatConsts.XNameLink, new XAttribute("href", pathToScripts + "hocr.css"), new XAttribute("rel", "stylesheet")),
							new XElement(HocrFormatConsts.XNameScript, new XAttribute("src", pathToScripts + "jquery.js"), " "),
							new XElement(HocrFormatConsts.XNameScript, new XAttribute("src", pathToScripts + "hocr.js"), " "),
							MakeXElementWithAttributes(HocrFormatConsts.XNameMeta, new Dictionary<string, string>()
								{
									{"http-equiv", "Content-Type"},
									{"content", "text/html; charset=utf-8"}
								}),
							MakeMetaElement("ocr-system", "kontur-recognition"),
							MakeMetaElement("ocr-capabilities", "ocr_page ocr_carea ocr_par ocr_line ocrx_word ocrp_wconf ocr_separator ocr_table")
							),
						bodyElement
					)
				);
			var pageElement = 
				MakeXElementWithAttributes(
					HocrFormatConsts.XNameDiv, new Dictionary<string, string>()
					{
						{"class", HocrFormatConsts.ClassPage},
						{"id", "page_1"},
						{"title", string.Format("{0}; ppageno 0", FormatTitleAttribute(model.PageBox))},
						{"gridunit", string.Format("{0}", model.GridUnit.Divisor)}
					});

			bodyElement.Add(pageElement);

			var idCounter = new IdCounter();

			foreach (var textBlock in model.TextBlocks())
			{
				pageElement.Add(MakeTextBlockElement(textBlock, idCounter));
			}
			foreach (var separator in model.Separators())
			{
				var separatorElement = MakeXElementWithAttributes(
					HocrFormatConsts.XNameDiv, new Dictionary<string, string>()
						{
							{"class", HocrFormatConsts.ClassSeparator},
							{"id", idCounter.NextSeparatorId},
							{"title", FormatTitleAttribute(separator.BoundingBox)},
							{"separator", string.Format("{0} {1} {2} {3} {4}", 
								separator.StartPointX, separator.StartPointY,
								separator.EndPointX, separator.EndPointY, separator.Width)},
						});
				pageElement.Add(separatorElement);
			}
			foreach (GMTable table in model.Tables())
			{
				var tableElement = MakeXElementWithAttributes(
					HocrFormatConsts.XNameDiv, new Dictionary<string, string>()
						{
							{"class", HocrFormatConsts.ClassTable},
							{"id", idCounter.NextTableId},
							{"title", FormatTitleAttribute(table.BoundingBox)},
							{HocrFormatConsts.AttrTableRowsCount, table.RowsCount.ToString()},
							{HocrFormatConsts.AttrTableColsCount, table.ColsCount.ToString()}
						});
				foreach (var cell in table.Cells())
					tableElement.Add(MakeCellElement(cell, idCounter));
				pageElement.Add(tableElement);
			}

			return document;
		}

		public static void StoreModel(TextGeometryModel geometryModel, string targetPath, string targetFileName)
		{
			if (string.IsNullOrEmpty(targetPath))
			{
				targetPath = ".";
			}
			StoreHOCRVisualizerScripts(targetPath);
			var xmlDoc = ProcessModel(geometryModel, "./");
			using (
				var stream = File.Open(Path.Combine(targetPath, targetFileName), FileMode.Create, FileAccess.Write,
				                       FileShare.Read))
			{
				StoreXmlDoc(xmlDoc, stream);
			}
		}

		public static void StoreHOCRVisualizerScripts(string targetPath)
		{
			if (string.IsNullOrEmpty(targetPath))
			{
				targetPath = ".";
			}
			var filesToSave = new[]{"hocr.css", "hocr.js", "jquery.js"};
			foreach (var fileName in filesToSave)
			{
				StoreEmbeddedFile(fileName, targetPath);
			}
		}

		public static void StoreModel(TextGeometryModel geometryModel, Stream target)
		{
			var xmlDoc = ProcessModel(geometryModel, "./");
			StoreXmlDoc(xmlDoc, target);
		}

		private static void StoreXmlDoc(XDocument xmlDoc, Stream target)
		{
			using (var writer = XmlWriter.Create(target, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
			{
				xmlDoc.WriteTo(writer);
				writer.Flush();
			}
			
		}

		private static void StoreEmbeddedFile(string fileName, string targetPath)
		{
			var data = EmbeddedResource.ReadBytes(typeof (HocrFormatProducer), fileName);
			var targetFile = Path.Combine(targetPath, fileName);
			File.WriteAllBytes(targetFile, data);
		}

		private static XElement MakeCellElement(GMTableCell cell, IdCounter idCounter)
		{
			var cellElement = MakeXElementWithAttributes(
				HocrFormatConsts.XNameDiv, new Dictionary<string, string>()
					{
						{"class", HocrFormatConsts.ClassTableCell},
						{"id", idCounter.NextTextBlockId},
						{HocrFormatConsts.AttrCellRowIndex, cell.RowIndex.ToString()},
						{HocrFormatConsts.AttrCellColIndex, cell.ColIndex.ToString()},
						{HocrFormatConsts.AttrCellRowSpan, cell.RowSpan.ToString()},
						{HocrFormatConsts.AttrCellColSpan, cell.ColSpan.ToString()},
					});
			cellElement.Add(MakeTextBlockElement(cell.TextBlock, idCounter));
			return cellElement;
			
		}

		private static XElement MakeTextBlockElement(GMTextBlock textBlock, IdCounter idCounter)
		{
			var textBlockElement = MakeXElementWithAttributes(
				HocrFormatConsts.XNameDiv, new Dictionary<string, string>()
					{
						{"class", HocrFormatConsts.ClassTextBlock},
						{"id", idCounter.NextTextBlockId},
						{"title", FormatTitleAttribute(textBlock.BoundingBox)}
					});

			foreach (var paragraph in textBlock.Paragraphs())
			{
				var paragraphElement = MakeXElementWithAttributes(
					HocrFormatConsts.XNameP, new Dictionary<string, string>()
						{
							{"class", HocrFormatConsts.ClassParagraph},
							{"dir", "ltr"},
							{"id", idCounter.NextParagraphId},
							{"title", FormatTitleAttribute(paragraph.BoundingBox)}
						});
				textBlockElement.Add(paragraphElement);

				foreach (var line in paragraph.Lines())
				{
					var lineElement = MakeXElementWithAttributes(
						HocrFormatConsts.XNameSpan, new Dictionary<string, string>()
							{
								{"class", HocrFormatConsts.ClassLine},
								{"id", idCounter.NextLineId},
								{"title", FormatTitleAttribute(line.BoundingBox)}
							});
					paragraphElement.Add(lineElement);

					foreach (var word in line.Words())
					{
						var wordElement = MakeXElementWithAttributes(
							HocrFormatConsts.XNameSpan, new Dictionary<string, string>()
								{
									{"class", HocrFormatConsts.ClassWord},
									{"id", idCounter.NextWordId},
									{"title", FormatTitleAttribute(word.BoundingBox, word.Accuracy)},
									{"accuracy", string.Format(CultureInfo.InvariantCulture, "{0:N2}", word.Accuracy/100.0)}
								});
						lineElement.Add(wordElement);

						wordElement.Add(
							new XElement(HocrFormatConsts.XNameStrong, word.Text));
					}
				}
				// TODO: store information about standalone words (they should be split onto lines)
			}
			return textBlockElement;
		}

		private static string FormatTitleAttribute(BoundingBox bbox, int? wordConfidence = null)
		{
			var result = new StringBuilder();
			result.AppendFormat("bbox {0} {1} {2} {3}", bbox.XMin, bbox.YMin, bbox.XMax, bbox.YMax);
			if (wordConfidence.HasValue)
			{
				result.AppendFormat("; x_wconf {0}", wordConfidence.Value);
			}
			return result.ToString();
		}

		private static XElement MakeMetaElement(string nameValue, string contentValue)
		{
			var result = new XElement(HocrFormatConsts.XNameMeta);
			result.SetAttributeValue("name", nameValue);
			result.SetAttributeValue("content", contentValue);
			return result;
		}

		internal static XElement MakeXElementWithAttributes(XName name, Dictionary<string, string> attributes, params object[] content)
		{
			var result = new XElement(name, content);
			foreach (var entry in attributes)
			{
				result.SetAttributeValue(entry.Key, entry.Value);
			}
			return result;
		}

		private class IdCounter
		{
			private int textBlockId;
			private int paragraphId;
			private int lineId;
			private int wordId;
			private int separatorId;
			private int tableId;

			public string NextTextBlockId { get { return string.Format("block_{0}", ++textBlockId); } }
			public string NextParagraphId { get { return string.Format("par_{0}", ++paragraphId); } }
			public string NextLineId { get { return string.Format("line_{0}", ++lineId); } }
			public string NextWordId { get { return string.Format("word_{0}", ++wordId); } }
			public string NextSeparatorId { get { return string.Format("separator_{0}", ++separatorId); } }
			public string NextTableId { get { return string.Format("table_{0}", ++tableId); } }
		}
	}
}