using System.Collections.Generic;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	public class GMLine : GMElement
	{
		private readonly List<GMWord> words = new List<GMWord>();

		public GMLine([NotNull] BoundingBox boundingBox)
			: base(boundingBox)
		{
		}

		public IEnumerable<GMWord> Words()
		{
			return words;
		}

		public void AddWord(GMWord word)
		{
			words.Add(word);
		}
	}
}