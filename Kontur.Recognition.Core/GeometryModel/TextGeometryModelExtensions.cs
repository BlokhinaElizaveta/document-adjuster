using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kontur.Recognition.GeometryModel
{
	public static class TextGeometryModelExtensions
	{
		private const string HTMLDebugHeader =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"" lang=""en"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<style>
div.wordbox {border: solid 1px red; color:Red; font-size: 70%;}
div.wordbox span:hover {color:Blue; font-size: 150%; background-color: yellow;}
div.textbox {border: dotted 1px green;}
div.parabox {border: dotted 1px green;}
div.parabox-text {font-size: 100%; border: dotted 1px green; background-color: rgba(200,200,0,0.1); color: rgba(0,0,0,0);}
div.parabox-text:hover {color:Blue; font-size: 100%; background-color: yellow;}
div.parabox-text span {font-size: 100%; }
div.linebox {border: dotted 1px blue;}
div.linebox-bad {border: solid 3px blue;}
div.tablebox {border: solid 5px #8A2BE2;}
</style>
</head>
 <body>";

		private const string HTMLDebugFooter = 
@" </body>
</html>";

		private const string HTMLDebugWordBoxTemplate =
@"<div class=""wordbox"" style=""position: absolute; left: {0}px; top: {1}px; width: {2}px; height: {3}px; {5}"">
     <span class=""wordtext"">{4}</span>
</div>" + "\n";

		private const string HTMLDebugLineBoxTemplate =
@"<div class=""linebox"" style=""position: absolute; left: {0}px; top: {1}px; width: {2}px; height: {3}px;""></div>" + "\n";

		private const string HTMLDebugParagraphBoxTemplate =
@"<div class=""parabox"" style=""position: absolute; left: {0}px; top: {1}px; width: {2}px; height: {3}px;""></div>" + "\n";

		private const string HTMLDebugParagraphBoxWithTextTemplate =
@"<div class=""parabox-text"" style=""position: absolute; left: {0}px; top: {1}px; width: {2}px; height: {3}px; {5}"">
     <span class=""wordtext"">{4}</span>
</div>" + "\n";

		private const string HTMLDebugTextBoxTemplate =
@"<div class=""textbox"" style=""position: absolute; left: {0}px; top: {1}px; width: {2}px; height: {3}px;""></div>" + "\n";

		private const string HTMLDebugImageBoxTemplate =
@"<div class=""imagebox"" style=""position: absolute; left: 0px; top: 0px; width: {0}px; height: {1}px;"">
<img border=""0"" src=""{2}"" width=""{0}"" height=""{1}""/>
</div>" + "\n";
		
		private const string HTMLDebugTableBoxTemplate =
@"<div class=""tablebox"" style=""position: absolute; left: {0}px; top: {1}px; width: {2}px; height: {3}px;""></div>" + "\n";

		public static string ToHtml(this TextGeometryModel model, string imageFileName = null)
		{
			var result = new StringBuilder(100000);
			result.Append(HTMLDebugHeader).Append("\n");
			if (imageFileName != null)
			{
				result.Append(string.Format(HTMLDebugImageBoxTemplate, model.PageBox.Width, model.PageBox.Height, imageFileName));
			}
			foreach (var textBlock in model.TextBlocks())
			{
				AppendTextBlock(textBlock, result);
			}
			foreach (var gmTable in model.Tables())
			{
				var bbox = gmTable.BoundingBox;
				result.Append(string.Format(HTMLDebugTableBoxTemplate, bbox.XMin, bbox.YMin, bbox.Width, bbox.Height));

				foreach (var textBlock in gmTable.Cells().Select(c => c.TextBlock))
				{
					AppendTextBlock(textBlock, result);
				}
			}
			foreach (var word in model.Words())
			{
				var bbox = word.BoundingBox;
				result.Append(string.Format(HTMLDebugWordBoxTemplate, bbox.XMin, bbox.YMin, bbox.Width, bbox.Height, word.Text, GetBackgroundStyle(word.Accuracy)));
			}
			result.Append(HTMLDebugFooter).Append("\n");
			return result.ToString().Replace("&", "&amp;");
		}

		private static void AppendTextBlock(GMTextBlock textBlock, StringBuilder result)
		{
			var bbox = textBlock.BoundingBox;
			result.Append(string.Format(HTMLDebugTextBoxTemplate, bbox.XMin, bbox.YMin, bbox.Width, bbox.Height));

			foreach (var paragraph in textBlock.Paragraphs())
			{
				bbox = paragraph.BoundingBox;
				result.Append(string.Format(HTMLDebugParagraphBoxTemplate, bbox.XMin, bbox.YMin, bbox.Width, bbox.Height));

				foreach (var line in paragraph.Lines())
				{
					bbox = line.BoundingBox;
					result.Append(string.Format(HTMLDebugLineBoxTemplate, bbox.XMin, bbox.YMin, bbox.Width, bbox.Height));
				}
			}
		}

		public static string ToHtmlParagraphs(this TextGeometryModel model, string imageFileName = null) 
		{
			var result = new StringBuilder(100000);
			result.Append(HTMLDebugHeader).Append("\n");
			if (imageFileName != null)
			{
				result.Append(string.Format(HTMLDebugImageBoxTemplate, model.PageBox.Width, model.PageBox.Height, imageFileName));
			}
			foreach (var textBlock in model.AllTextBlocks())
			{
				var bbox = textBlock.BoundingBox;
				result.Append(string.Format(HTMLDebugTextBoxTemplate, bbox.XMin, bbox.YMin, bbox.Width, bbox.Height));

				foreach (var paragraph in textBlock.Paragraphs())
				{
					bbox = paragraph.BoundingBox;
					var text = paragraph.AsText(true);
					result.Append(string.Format(HTMLDebugParagraphBoxWithTextTemplate, bbox.XMin, bbox.YMin, bbox.Width, bbox.Height, text, ""));
				}
			}
			result.Append(HTMLDebugFooter).Append("\n");
			return result.ToString();
		}

		private static string GetBackgroundStyle(int accuracy)
		{
			return string.Format(@"background-color: rgba({0},{1},0,0.2);", (int)((100 - accuracy) * 2.54), (int)(accuracy * 2.54));
		}

		/// <summary>
		/// Returns all lines of text in given model
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public static IEnumerable<GMLine> Lines(this TextGeometryModel model)
		{
			return model.AllTextBlocks().SelectMany(textBlock => textBlock.Paragraphs()).SelectMany(para => para.Lines());
		}

		public static TextGeometryModel RemoveEmptyElements(this TextGeometryModel model)
		{
			var result = new TextGeometryModel(model.PageBox, model.GridUnit);
			foreach (var textBlock in model.TextBlocks())
			{
				var textBlockNew = textBlock.RemoveEmptyElements();
				if (textBlockNew.Paragraphs().Any() || textBlockNew.StandaloneWords().Any())
				{
					result.AddTextBlock(textBlockNew);
				}
			}
			foreach (var table in model.Tables().Select(RemoveEmptyElementsInTable))
			{
				result.AddTable(table);
			}
			return result;
		}

		private static GMTable RemoveEmptyElementsInTable(this GMTable table)
		{
			return new GMTable(
				table.BoundingBox,
				table.RowsCount, table.ColsCount,
				table.Cells().Select(RemoveEmptyElementsInCell));
		}

		private static GMTableCell RemoveEmptyElementsInCell(GMTableCell cell)
		{
			var newTextBlock = cell.TextBlock.RemoveEmptyElements();
			return new GMTableCell(
				newTextBlock,
				cell.RowIndex, cell.ColIndex,
				cell.RowSpan, cell.ColSpan);
		}

		private static GMTextBlock RemoveEmptyElements(this GMTextBlock textBlock)
		{
			var textBlockNew = new GMTextBlock(textBlock.BoundingBox);
			foreach (var paragraph in textBlock.Paragraphs())
			{
				var paragraphNew = new GMParagraph(paragraph.BoundingBox);
				foreach (var line in paragraph.Lines())
				{
					var lineNew = new GMLine(line.BoundingBox);
					foreach (var word in line.Words())
					{
						if (word.Text.Trim().Length > 0)
						{
							var wordNew = new GMWord(word.BoundingBox, word.Text, word.Accuracy);
							lineNew.AddWord(wordNew);
						}
					}
					if (lineNew.Words().Any())
					{
						paragraphNew.AddLine(lineNew);
					}
				}
				if (paragraphNew.Lines().Any())
				{
					textBlockNew.AddParagraph(paragraphNew);
				}
				foreach (var word in textBlock.StandaloneWords())
				{
					if (word.Text.Trim().Length > 0)
					{
						var wordNew = new GMWord(word.BoundingBox, word.Text, word.Accuracy);
						textBlockNew.AddStandaloneWord(wordNew);
					}
				}
			}
			return textBlockNew;
		}


		public static int MinAccuracy(this GMParagraph paragraph)
		{
			var minAccuracy = paragraph.Lines().SelectMany(line => line.Words()).Aggregate(100, (curMin, word) => (word.Accuracy < curMin ? word.Accuracy : curMin));
			return minAccuracy;
		}

		public static string AsText(this GMParagraph paragraph, bool useLF = true)
		{
			var buffer = new StringBuilder();
			foreach (var line in paragraph.Lines())
			{
				if (buffer.Length > 0)
				{
					buffer.Append(useLF ? '\n' : ' ');
				}
				var words = line.Words().ToList();
				if (words.Count > 0)
				{
					double sumWidth = 0;
					var charCount = 0;
					foreach (var word in words)
					{
						sumWidth += word.BoundingBox.Width;
						charCount += word.Text.Length;
					}
					var averageWidth = sumWidth / charCount;

					buffer.Append(words[0].Text);
					for (var iIdx = 1; iIdx < words.Count; iIdx++)
					{
						var curWord = words[iIdx];
						var distance = HDistance(words[iIdx - 1].BoundingBox, curWord.BoundingBox);
						var spacesCount = (int)Math.Round(distance / averageWidth);
						if (spacesCount == 0)
						{
							spacesCount = 1;
						}
						buffer.Append(new string(' ', spacesCount));
						buffer.Append(curWord.Text);
					}
				}
			}
			return buffer.ToString();
		}

		// Returns horizontal distance between given rectangle boxes. If they overlap, returns 0
		private static int HDistance(BoundingBox box1, BoundingBox box2)
		{
			if (box1.XMax <= box2.XMin)
			{
				return box2.XMin - box1.XMax;
			}
			if (box2.XMax <= box1.XMin)
			{
				return box1.XMin - box2.XMax;
			}
			return 0;
		}
	}
}