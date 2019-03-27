using System.Collections.Generic;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	public class GMParagraph : GMElement
	{
		private readonly List<GMLine> lines = new List<GMLine>();

		public GMParagraph([NotNull] BoundingBox boundingBox)
			: base(boundingBox)
		{
		}

		public IEnumerable<GMLine> Lines()
		{
			return lines;
		}

		public void AddLine(GMLine line)
		{
			lines.Add(line);
		}

		public GMLine AddLine([NotNull] BoundingBox boundingBox)
		{
			var line = new GMLine(boundingBox);
			lines.Add(line);
			return line;
		}

	}
}