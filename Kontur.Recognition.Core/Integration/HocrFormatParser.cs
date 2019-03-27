using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Kontur.Recognition.GeometryModel;
using Kontur.Recognition.Utils;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Integration
{
	/// <summary>
	/// This class implement logic to parse data from HOCR format (produced by Tesseract OCR) into geometric model representation
	/// </summary>
	public static class HocrFormatParser
	{
		// Regexp to parse coordinates of bounding rectangle.
		// According to model rules they must be positive. But it is still possible that certain model contains negative coordinates
		private static readonly Regex titleAttrParser = new Regex(@"bbox (-?\d+) (-?\d+) (-?\d+) (-?\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static readonly Regex wconfAttrParser = new Regex(@"x_wconf (\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private static readonly char[] attrSeparators = new[] { ' ', '\t', '\n', '\r' };

		/// <summary>
		/// Parses the given HOCR model
		/// </summary>
		/// <param name="path">The path to the file with a model content to parse</param>
		/// <returns></returns>
		public static IEnumerable<TextGeometryModel> ParseHocrResults(string path)
		{
			return ParseHocrResults(XMLUtils.OpenXMLDocument(path));
		}

		/// <summary>
		/// Parses the given HOCR model
		/// </summary>
		/// <param name="path">The path to the file with a model content to parse</param>
		/// <param name="encoding">The character encoding to use to decode the stream (autodetected if specified as null)</param>
		/// <returns></returns>
		public static IEnumerable<TextGeometryModel> ParseHocrResults(string path, [NotNull] Encoding encoding)
		{
			return ParseHocrResults(XMLUtils.OpenXMLDocument(path, encoding));
		}

		/// <summary>
		/// Parses the given HOCR model
		/// </summary>
		/// <param name="hocrDataStream">The stream with a HOCR model content</param>
		/// <returns></returns>
		public static IEnumerable<TextGeometryModel> ParseHocrResults(Stream hocrDataStream)
		{
			return ParseHocrResults(XMLUtils.OpenXMLDocument(hocrDataStream));
		}

		/// <summary>
		/// Parses the given HOCR model
		/// </summary>
		/// <param name="hocrDataStream">The stream with a HOCR model content</param>
		/// <param name="encoding">The character encoding to use to decode the stream (autodetected if specified as null)</param>
		/// <returns></returns>
		public static IEnumerable<TextGeometryModel> ParseHocrResults(Stream hocrDataStream, [NotNull] Encoding encoding)
		{
			return ParseHocrResults(XMLUtils.OpenXMLDocument(hocrDataStream, encoding));
		}

		/// <summary>
		/// Main entry point to HOCR format parser.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="defaultGridUnit">Grid unit which will be stored in model in case the source HOCR file does not contain one</param>
		/// <returns></returns>
		public static IEnumerable<TextGeometryModel> ParseHocrResults(XContainer document, GridUnit defaultGridUnit = null)
		{
			return document.PageElements()
				.Select(pageElement => pageElement.ParsePage(defaultGridUnit));
		}

		private static bool HasClass([NotNull] this XElement element, [NotNull] string attValue)
		{
			var xAttribute = element.Attribute("class");
			return xAttribute != null && xAttribute.Value == attValue;
		}

		private static IEnumerable<XElement> ChildrenOfClass([NotNull] this XElement element, [NotNull] XName name, [NotNull] string className)
		{
			return element.Elements(name).Where(el => el.HasClass(className));
		}


		private static IEnumerable<XElement> PageElements([NotNull] this XContainer document)
		{
			return document.Descendants(HocrFormatConsts.XNameBody).First()
				.ChildrenOfClass(HocrFormatConsts.XNameDiv, HocrFormatConsts.ClassPage);
		}

		private static IEnumerable<XElement> TextBlockElements([NotNull] this XElement pageElement)
		{
			return pageElement
				.ChildrenOfClass(HocrFormatConsts.XNameDiv, HocrFormatConsts.ClassTextBlock);
		}

		private static IEnumerable<XElement> ParagraphElements([NotNull] this XElement textBlockElement)
		{
			return textBlockElement
				.ChildrenOfClass(HocrFormatConsts.XNameP, HocrFormatConsts.ClassParagraph);
		}

		private static IEnumerable<XElement> LineElements([NotNull] this XElement paragraphElement)
		{
			return paragraphElement
				.ChildrenOfClass(HocrFormatConsts.XNameSpan, HocrFormatConsts.ClassLine);
		}

		private static IEnumerable<XElement> WordElements([NotNull] this XElement lineElement)
		{
			return lineElement
				.ChildrenOfClass(HocrFormatConsts.XNameSpan, HocrFormatConsts.ClassWord);
		}

		private static IEnumerable<XElement> SeparatorElements([NotNull] this XElement pageElement)
		{
			return pageElement
				.ChildrenOfClass(HocrFormatConsts.XNameDiv, HocrFormatConsts.ClassSeparator);
		}

		private static IEnumerable<XElement> TableElements([NotNull] this XElement pageElement)
		{
			return pageElement
				.ChildrenOfClass(HocrFormatConsts.XNameDiv, HocrFormatConsts.ClassTable);
		}

		private static IEnumerable<XElement> TableCellElements([NotNull] this XElement pageElement)
		{
			return pageElement
				.ChildrenOfClass(HocrFormatConsts.XNameDiv, HocrFormatConsts.ClassTableCell);
		}

		private static string AttributeValue([NotNull] this XElement element, XName name)
		{
			var attr = element.Attribute(name);
			return attr != null ? attr.Value : null;
		}

		private static BoundingBox ParseBBoxAttribute([NotNull] this XElement element)
		{
			var titleAttrValue = element.AttributeValue("title") ?? "bbox 0 0 0 0";
			var match = titleAttrParser.Match(titleAttrValue);

			var xMin = Int32.Parse(match.Groups[1].Value);
			var yMin = Int32.Parse(match.Groups[2].Value);
			var xMax = Int32.Parse(match.Groups[3].Value);
			var yMax = Int32.Parse(match.Groups[4].Value);
			return new BoundingBox(xMin, yMin, xMax, yMax);
		}

		private static int? TryParseWordConfidenceAttribute([NotNull] this XElement element)
		{
			var titleAttrValue = element.AttributeValue("title") ?? "";
			var match = wconfAttrParser.Match(titleAttrValue);
			if (match.Success)
			{
				return int.Parse(match.Groups[1].Value, NumberStyles.Integer);
			}

			return null;
		}

		private static int[] ParseIntCollectionAttribute([NotNull] this XElement element, string attrName)
		{
			return (element.AttributeValue(attrName) ?? "")
				.Split(attrSeparators, StringSplitOptions.RemoveEmptyEntries)
				.Select(int.Parse)
				.ToArray();
		}

		private static int ParseIntAttribute([NotNull] this XElement element, string attrName)
		{
			return int.Parse(element.AttributeValue(attrName), NumberFormatInfo.InvariantInfo);
		}

		private static int? TryParseOptionalIntAttribute([NotNull] this XElement element, string attrName)
		{
			var attributeValue = element.AttributeValue(attrName);
			return attributeValue != null ? int.Parse(attributeValue, NumberFormatInfo.InvariantInfo) : (int?)null;
		}

		private static double ParseOptionalDoubleAttribute([NotNull] this XElement element, string attrName, double defaultValue)
		{
			var attributeValue = element.AttributeValue(attrName);
			return attributeValue != null ? double.Parse(attributeValue, NumberFormatInfo.InvariantInfo) : defaultValue;
		}

		private static double? TryParseOptionalDoubleAttribute([NotNull] this XElement element, string attrName)
		{
			var attributeValue = element.AttributeValue(attrName);
			return attributeValue != null ? double.Parse(attributeValue, NumberFormatInfo.InvariantInfo) : (double?)null;
		}

		private static GridUnit ParseGridUnitAttribute([NotNull] this XElement element)
		{
			var attr = element.Attribute("gridunit");
			if (attr == null)
				return GridUnit.UNKNOWN_UNITS;
			return GridUnit.ByResolution(Int32.Parse(attr.Value));
		}

		private static TextGeometryModel ParsePage([NotNull] this XElement pageElement, [CanBeNull] GridUnit defaultGridUnit)
		{
			var boundingBox = pageElement.ParseBBoxAttribute();
			var gridUnit = pageElement.ParseGridUnitAttribute();
			if (GridUnit.UNKNOWN_UNITS.Equals(gridUnit) && defaultGridUnit != null)
			{
				gridUnit = defaultGridUnit;
			}
			var model = new TextGeometryModel(boundingBox, gridUnit);
			ParseTextBlocks(pageElement, model);
			ParseSeparators(pageElement, model);
			ParseTables(pageElement, model);
			return model;
		}

		private static void ParseTextBlocks(XElement pageElement, TextGeometryModel model)
		{
			foreach (var textBlockElement in pageElement.TextBlockElements())
			{
				var textBlock = textBlockElement.ParseTextBlock();
				if (!textBlock.IsEmpty())
				{
					model.AddTextBlock(textBlock);
				}
			}
		}

		private static void ParseSeparators(XElement pageElement, TextGeometryModel model)
		{
			foreach (var separatorElement in pageElement.SeparatorElements())
			{
				var separator = separatorElement.ParseSeparator();
				model.AddSeparator(separator);
			}
		}

		private static void ParseTables(XElement pageElement, TextGeometryModel model)
		{
			foreach (var tableElement in pageElement.TableElements())
			{
				var table = tableElement.ParseTable();
				model.AddTable(table);
			}
		}

		private static GMTextBlock ParseTextBlock([NotNull] this XElement textBlockElement)
		{
			var boundingBox = textBlockElement.ParseBBoxAttribute();
			var textBlock = new GMTextBlock(boundingBox);
			foreach (var paraElement in textBlockElement.ParagraphElements())
			{
				var paragraph = paraElement.ParseParagraph();
				if (!paragraph.IsEmpty())
				{
					textBlock.AddParagraph(paragraph);
				}
			}
			return textBlock;
		}

		private static GMParagraph ParseParagraph([NotNull] this XElement paragraphElement)
		{
			var boundingBox = paragraphElement.ParseBBoxAttribute();
			var paragraph = new GMParagraph(boundingBox);
			foreach (var lineElement in paragraphElement.LineElements())
			{
				var line = lineElement.ParseLine();
				if (!line.IsEmpty())
				{
					paragraph.AddLine(line);
				}
			}
			return paragraph;
		}

		private static GMLine ParseLine([NotNull] this XElement lineElement)
		{
			var boundingBox = lineElement.ParseBBoxAttribute();
			var line = new GMLine(boundingBox);
			foreach (var wordElement in lineElement.WordElements())
			{
				var word = wordElement.ParseWord();
				if (!word.IsEmpty())
				{
					line.AddWord(word);
				}
			}
			return line;
		}

		private static GMWord ParseWord([NotNull] this XElement wordElement)
		{
			var boundingBox = wordElement.ParseBBoxAttribute();
			var wordConfidence = wordElement.TryParseWordConfidenceAttribute();
			var accuracy = wordConfidence ?? (int)Math.Round(wordElement.ParseOptionalDoubleAttribute("accuracy", 1) * 100);
			return new GMWord(boundingBox, wordElement.Value, accuracy);
		}

		private static GMSeparator ParseSeparator([NotNull] this XElement separatorElement)
		{
			var boundingBox = separatorElement.ParseBBoxAttribute();
			var vals = separatorElement.ParseIntCollectionAttribute("separator");
			return new GMSeparator(boundingBox, vals[0], vals[1], vals[2], vals[3], vals[4]);
		}

		private static GMTable ParseTable([NotNull] this XElement tableElement)
		{
			var boundingBox = tableElement.ParseBBoxAttribute();
			if (tableElement.Attribute("hseparators") != null)
			{
				// переходный формат. После конвернтации всего, надо будет убить.
				var hSeparators = tableElement.ParseIntCollectionAttribute("hseparators");
				var vSeparators = tableElement.ParseIntCollectionAttribute("vseparators");
				var textBlocks =
					tableElement.TextBlockElements()
					            .Select(tb => tb.ParseTextBlock())
					            .Where(tb => !tb.IsEmpty());
				return new GMTable(boundingBox, hSeparators, vSeparators, textBlocks);
			}
			else
			{
				var rowsCount = tableElement.ParseIntAttribute(HocrFormatConsts.AttrTableRowsCount);
				var colsCount = tableElement.ParseIntAttribute(HocrFormatConsts.AttrTableColsCount);
				var cells = tableElement.TableCellElements().Select(ParseTableCell);
				return new GMTable(boundingBox, rowsCount, colsCount, cells);
			}
		}

		private static GMTableCell ParseTableCell(XElement cellElement)
		{
			var rowIndex = cellElement.ParseIntAttribute(HocrFormatConsts.AttrCellRowIndex);
			var rowSpan = cellElement.ParseIntAttribute(HocrFormatConsts.AttrCellRowSpan);
			var colIndex = cellElement.ParseIntAttribute(HocrFormatConsts.AttrCellColIndex);
			var colSpan = cellElement.ParseIntAttribute(HocrFormatConsts.AttrCellColSpan);
			var tb = cellElement.TextBlockElements().Single().ParseTextBlock();
			return new GMTableCell(tb, rowIndex, colIndex, rowSpan, colSpan);
		}
	}
}