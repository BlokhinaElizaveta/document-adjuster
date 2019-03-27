using System;
using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.GeometryModel;

namespace Kontur.Recognition.Utils
{
    /// <summary>
    /// TextGeometryModel in special format: BoundingBox divides into fixed parts, each part holds words which arrange in part's boundary.
    /// </summary>
    public class LinesHashInGeometryModel
    {
		private readonly List<GMWord>[] hash;
        private readonly int yEpsilon;
        private readonly int xEpsilon;
	    private const int segmentSize = 20;

	    public LinesHashInGeometryModel(IEnumerable<GMWord> words, int yEpsilon, int xEpsilon, BoundingBox box)
        {
            hash = Enumerable.Range(0, box.Height / 20).Select(x => new List<GMWord>()).ToArray();
            this.yEpsilon = yEpsilon;
            this.xEpsilon = xEpsilon;
            foreach (var word in words)
            {
                AddWord(word);
            }
        }

        private void AddWord(GMWord word)
        {
            int startIndex = GetStartIndex(word);
            int endIndex = GetEndIndex(word);
            for (int index = startIndex; index <= endIndex; index++)
            {
                hash[index].Add(word);
            }
        }

        public bool Contains(GMWord word, Func<string, string, int> comparator = null)
        {
            if (comparator == null)
                comparator = (w1, w2) => String.Compare(w1, w2, StringComparison.OrdinalIgnoreCase);
            var startIndex = GetStartIndex(word);
            var endIndex = GetEndIndex(word);
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (startIndex >= hash.Length || endIndex >= hash.Length)
                    continue;
                if (hash[i].Any(w => EqualsBoxes(w.BoundingBox, word.BoundingBox) && comparator(w.Text, word.Text) == 0))//String.Compare(w.Text, word.Text, StringComparison.OrdinalIgnoreCase) == 0))
                    return true;
            }
            return false;
        }

        private int GetEndIndex(GMWord word)
        {
            int endIndex = (word.BoundingBox.YMax + yEpsilon)/segmentSize;
            if (endIndex >= hash.Count())
                endIndex = hash.Count() - 1;
            return endIndex;
        }

        private int GetStartIndex(GMWord word)
        {
            int startIndex = (word.BoundingBox.YMin - yEpsilon)/segmentSize;
            if (startIndex < 0)
                startIndex = 0;
            return startIndex;
        }

        private bool EqualsBoxes(BoundingBox box1, BoundingBox box2)
        {
            if ((Math.Abs(box1.CenterX - box2.CenterX) < xEpsilon)
                && (Math.Abs(box1.CenterY - box2.CenterY) < yEpsilon)
                && (Math.Abs(box1.Width - box2.Width) < xEpsilon * 2)
                && (Math.Abs(box1.Height - box2.Height) < yEpsilon * 2))
                return true;
            return false;
        }
    }
}
