using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	public class GMTableCell
	{
		public GMTableCell([NotNull] GMTextBlock textBlock, int rowIndex, int colIndex, int rowSpan, int colSpan)
		{
			TextBlock = textBlock;
			RowIndex = rowIndex;
			ColIndex = colIndex;
			RowSpan = rowSpan;
			ColSpan = colSpan;
		}

		[NotNull]
		public GMTextBlock TextBlock { get; set; }
		public int RowIndex { get; set; }
		public int ColIndex { get; set; }
		public int RowSpan { get; set; }
		public int ColSpan { get; set; }
	}
}