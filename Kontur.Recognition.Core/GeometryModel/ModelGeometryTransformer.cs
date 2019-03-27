using System;
using System.Linq;

namespace Kontur.Recognition.GeometryModel
{
	public static class ModelGeometryTransformer
	{
		public static TextGeometryModel TransformModelGeometry(TextGeometryModel sourceModel, TextGeometryModel targetModel, Func<BoundingBox, BoundingBox> boxConverter)
		{
			foreach (var textBlock in sourceModel.TextBlocks().Select(textBlock => TransformTextBlock(textBlock, boxConverter)))
			{
				targetModel.AddTextBlock(textBlock);
			}
			foreach (var table in sourceModel.Tables().Select(table => TransformTable(table, boxConverter)))
			{
				targetModel.AddTable(table);
			}
			return targetModel;
		}

		private static GMTable TransformTable(GMTable table, Func<BoundingBox, BoundingBox> boxConverter)
		{
			return new GMTable(
				boxConverter(table.BoundingBox),
				table.RowsCount, table.ColsCount,
				table.Cells().Select(c => TransformCell(c, boxConverter)));
		}

		private static GMTableCell TransformCell(GMTableCell cell, Func<BoundingBox, BoundingBox> boxConverter)
		{
			return new GMTableCell(
				TransformTextBlock(cell.TextBlock, boxConverter),
				cell.RowIndex, cell.ColIndex,
				cell.RowSpan, cell.ColSpan);
		}

		private static GMTextBlock TransformTextBlock(GMTextBlock textBlock, Func<BoundingBox, BoundingBox> boxConverter)
		{
			var targetTextBlock = new GMTextBlock(boxConverter(textBlock.BoundingBox));
			foreach (var paragraph in textBlock.Paragraphs())
			{
				var targetParagraph = targetTextBlock.AddParagraph(boxConverter(paragraph.BoundingBox));
				foreach (var line in paragraph.Lines())
				{
					var targetLine = targetParagraph.AddLine(boxConverter(line.BoundingBox));
					foreach (var word in line.Words())
					{
						var targetWord = new GMWord(boxConverter(word.BoundingBox), word.Text, word.Accuracy);
						targetLine.AddWord(targetWord);
					}
				}
			}

			foreach (var word in textBlock.StandaloneWords())
			{
				var targetWord = new GMWord(boxConverter(word.BoundingBox), word.Text, word.Accuracy);
				targetTextBlock.AddStandaloneWord(targetWord);
			}
			return targetTextBlock;
		}
	}
}